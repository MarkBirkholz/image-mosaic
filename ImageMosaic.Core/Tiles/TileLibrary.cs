using System.Collections.Concurrent;
using System.Diagnostics;
using ImageMosaic.Core.Imaging;

namespace ImageMosaic.Core.Tiles;

public sealed record TileLibraryOptions(string Folder, int TileSize, int FeatureGrid = 3)
{
    /// <summary>Where prepared tiles are cached between runs; null disables the cache.</summary>
    public string? CacheDirectory { get; init; } = TileCache.DefaultDirectory;

    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
}

public sealed record TileLibraryStats(int FilesFound, int FromCache, int Decoded, int Failed, int Duplicates, TimeSpan Elapsed)
{
    public override string ToString() =>
        $"{FilesFound:N0} files: {FromCache:N0} from cache, {Decoded:N0} decoded, {Failed:N0} unreadable, " +
        $"{Duplicates:N0} duplicates skipped — {Elapsed.TotalSeconds:N1} s";
}

/// <summary>All usable tiles of a folder, prepared for one tile size.</summary>
public sealed class TileLibrary
{
    public TileLibrary(TileLibraryOptions options, IReadOnlyList<Tile> tiles, TileLibraryStats stats)
    {
        Options = options;
        Tiles = tiles;
        Stats = stats;
    }

    public TileLibraryOptions Options { get; }

    public int TileSize => Options.TileSize;

    public int FeatureGrid => Options.FeatureGrid;

    public IReadOnlyList<Tile> Tiles { get; }

    public TileLibraryStats Stats { get; }

    /// <summary>Builds a library from tiles already in memory (used by tests and tools).</summary>
    public static TileLibrary FromPixels(IEnumerable<PixelBuffer> tiles, int featureGrid = 3)
    {
        var list = tiles.Select((p, i) => new Tile($"tile{i}", p, featureGrid)).ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("At least one tile is required.", nameof(tiles));
        }

        var size = list[0].Pixels.Width;
        if (list.Any(t => t.Pixels.Width != size || t.Pixels.Height != size))
        {
            throw new ArgumentException("All tiles must be squares of the same size.", nameof(tiles));
        }

        var options = new TileLibraryOptions("(memory)", size, featureGrid) { CacheDirectory = null };
        return new TileLibrary(options, list, new TileLibraryStats(list.Count, 0, 0, 0, 0, TimeSpan.Zero));
    }

    /// <summary>
    /// Scans the folder, decodes photos in parallel (or takes them from the cache), crops each to a
    /// centered square, scales it to the tile size and skips pixel-identical duplicates.
    /// </summary>
    public static TileLibrary Load(
        TileLibraryOptions options,
        IProgress<MosaicProgress>? progress = null,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.TileSize, 4);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.FeatureGrid, 1);
        var stopwatch = Stopwatch.StartNew();

        progress?.Report(new MosaicProgress(MosaicStage.Scanning, 0, 0));
        var files = ImageFileScanner.Scan(options.Folder, cancellationToken);

        var cache = options.CacheDirectory != null ? new TileCache(options.CacheDirectory) : null;
        var cached = cache?.Load(options.Folder, options.TileSize)
                     ?? new Dictionary<string, CachedTile>(StringComparer.OrdinalIgnoreCase);

        var entries = new CachedTile[files.Count];
        var fromCache = 0;
        var decoded = 0;
        var failures = new ConcurrentBag<string>();
        var counter = new ThrottledProgress(progress, MosaicStage.LoadingTiles, files.Count);
        counter.Start();

        var parallel = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, options.MaxDegreeOfParallelism),
            CancellationToken = cancellationToken,
        };
        Parallel.For(0, files.Count, parallel, i =>
        {
            var path = files[i];
            var info = new FileInfo(path);
            long length = 0, ticks = 0;
            try
            {
                length = info.Length;
                ticks = info.LastWriteTimeUtc.Ticks;
            }
            catch (IOException)
            {
                // Vanished or locked file: treated as unreadable below.
            }

            if (cached.TryGetValue(path, out var hit) && hit.Length == length && hit.LastWriteUtcTicks == ticks)
            {
                entries[i] = hit;
                Interlocked.Increment(ref fromCache);
            }
            else
            {
                byte[]? pixels = null;
                try
                {
                    pixels = PrepareTile(path, options.TileSize).Data;
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    failures.Add($"{path}: {e.Message}");
                }

                entries[i] = new CachedTile(length, ticks, pixels);
                Interlocked.Increment(ref decoded);
            }

            counter.Increment();
        });

        if (cache != null && (decoded > 0 || cached.Count != files.Count))
        {
            var updated = new Dictionary<string, CachedTile>(files.Count, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < files.Count; i++)
            {
                updated[files[i]] = entries[i];
            }

            try
            {
                cache.Save(options.Folder, options.TileSize, updated);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                log?.Invoke($"Could not write tile cache: {e.Message}");
            }
        }

        foreach (var failure in failures.OrderBy(f => f).Take(10))
        {
            log?.Invoke($"Skipped {failure}");
        }

        if (failures.Count > 10)
        {
            log?.Invoke($"…and {failures.Count - 10} more unreadable files.");
        }

        var (tiles, duplicates) = BuildTiles(files, entries, options, cancellationToken);
        var failed = entries.Count(e => e.Pixels == null);
        var stats = new TileLibraryStats(files.Count, fromCache, decoded, failed, duplicates, stopwatch.Elapsed);
        return new TileLibrary(options, tiles, stats);
    }

    /// <summary>Turns a photo into a tile: decode at reduced size, crop the centered square, scale down.</summary>
    public static PixelBuffer PrepareTile(string path, int tileSize) =>
        ImageCodec.Load(path, minShortSide: tileSize * 2).CropCenter(1.0).Resize(tileSize, tileSize);

    private static (List<Tile> Tiles, int Duplicates) BuildTiles(
        IReadOnlyList<string> files,
        CachedTile[] entries,
        TileLibraryOptions options,
        CancellationToken cancellationToken)
    {
        var size = options.TileSize;
        var unique = new List<Tile>(files.Count);
        var seen = new Dictionary<int, List<byte[]>>();
        var duplicates = 0;
        for (var i = 0; i < files.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pixels = entries[i].Pixels;
            if (pixels == null)
            {
                continue;
            }

            var hash = new HashCode();
            hash.AddBytes(pixels);
            var key = hash.ToHashCode();
            if (!seen.TryGetValue(key, out var bucket))
            {
                seen[key] = bucket = [];
            }

            if (bucket.Any(other => other.AsSpan().SequenceEqual(pixels)))
            {
                duplicates++;
                continue;
            }

            bucket.Add(pixels);
            unique.Add(new Tile(files[i], new PixelBuffer(size, size, pixels), options.FeatureGrid));
        }

        return (unique, duplicates);
    }
}
