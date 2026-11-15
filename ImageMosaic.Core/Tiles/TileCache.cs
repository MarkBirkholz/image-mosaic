using System.Security.Cryptography;
using System.Text;

namespace ImageMosaic.Core.Tiles;

/// <summary>A prepared tile as stored in the cache. <see cref="Pixels"/> is null when the file could not be decoded.</summary>
public sealed record CachedTile(long Length, long LastWriteUtcTicks, byte[]? Pixels);

/// <summary>
/// Disk cache of prepared tiles, one file per (folder, tile size). An entry is reused while the
/// source file keeps the same size and modification time, so a second run over the same folder
/// does not decode any photos.
/// </summary>
public sealed class TileCache(string directory)
{
    // Bump when the way tiles are produced changes, to invalidate old caches.
    private const int FormatVersion = 1;
    private const int Magic = 0x43544D49; // "IMTC"

    public string Directory { get; } = directory;

    /// <summary>
    /// %LOCALAPPDATA%\ImageMosaic\TileCache, or the folder in the IMAGEMOSAIC_CACHE_DIR environment variable
    /// (used to keep test runs away from the real cache).
    /// </summary>
    public static string DefaultDirectory =>
        Environment.GetEnvironmentVariable("IMAGEMOSAIC_CACHE_DIR") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ImageMosaic", "TileCache");

    /// <summary>Number and total size of the cache files (all folders and tile sizes).</summary>
    public (int Files, long Bytes) GetUsage()
    {
        var files = CacheFiles();
        return (files.Count, files.Sum(f => f.Length));
    }

    /// <summary>
    /// Deletes every cache file. Photos are not touched; the next run decodes them again.
    /// Returns how many files were deleted and how many could not be (e.g. in use).
    /// </summary>
    public (int Deleted, int Failed) Clear()
    {
        int deleted = 0, failed = 0;
        foreach (var file in CacheFiles())
        {
            try
            {
                file.Delete();
                deleted++;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                failed++;
            }
        }

        return (deleted, failed);
    }

    private List<FileInfo> CacheFiles()
    {
        var directory = new DirectoryInfo(Directory);
        if (!directory.Exists)
        {
            return [];
        }

        // Only our own files: finished caches and leftovers of interrupted writes.
        return directory.EnumerateFiles("tiles_*.bin")
            .Concat(directory.EnumerateFiles("tiles_*.bin.tmp"))
            .DistinctBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string GetCacheFile(string folder, int tileSize)
    {
        var key = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..16];
        return Path.Combine(Directory, $"tiles_{hash}_{tileSize}.bin");
    }

    public Dictionary<string, CachedTile> Load(string folder, int tileSize)
    {
        var result = new Dictionary<string, CachedTile>(StringComparer.OrdinalIgnoreCase);
        var file = GetCacheFile(folder, tileSize);
        if (!File.Exists(file))
        {
            return result;
        }

        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            if (reader.ReadInt32() != Magic || reader.ReadInt32() != FormatVersion || reader.ReadInt32() != tileSize)
            {
                return result;
            }

            var pixelBytes = tileSize * tileSize * 3;
            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var path = reader.ReadString();
                var length = reader.ReadInt64();
                var ticks = reader.ReadInt64();
                var hasPixels = reader.ReadBoolean();
                var pixels = hasPixels ? reader.ReadBytes(pixelBytes) : null;
                if (pixels != null && pixels.Length != pixelBytes)
                {
                    throw new EndOfStreamException();
                }

                result[path] = new CachedTile(length, ticks, pixels);
            }
        }
        catch (Exception e) when (e is IOException or EndOfStreamException or UnauthorizedAccessException)
        {
            // A damaged cache is simply rebuilt.
            result.Clear();
        }

        return result;
    }

    public void Save(string folder, int tileSize, IReadOnlyDictionary<string, CachedTile> entries)
    {
        System.IO.Directory.CreateDirectory(Directory);
        var file = GetCacheFile(folder, tileSize);
        var temp = file + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
        using (var writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(tileSize);
            writer.Write(entries.Count);
            foreach (var (path, entry) in entries)
            {
                writer.Write(path);
                writer.Write(entry.Length);
                writer.Write(entry.LastWriteUtcTicks);
                writer.Write(entry.Pixels != null);
                if (entry.Pixels != null)
                {
                    writer.Write(entry.Pixels);
                }
            }
        }

        File.Move(temp, file, overwrite: true);
    }
}
