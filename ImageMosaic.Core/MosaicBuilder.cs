using System.Diagnostics;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Mosaic;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Core;

/// <summary>A finished mosaic with everything needed to re-render it with other color settings.</summary>
public sealed class MosaicResult(
    MosaicSettings settings,
    TileLibrary library,
    PixelBuffer target,
    TargetGrid grid,
    MosaicPlan plan,
    PixelBuffer image)
{
    public MosaicSettings Settings { get; } = settings;

    public TileLibrary Library { get; } = library;

    public PixelBuffer Target { get; } = target;

    public TargetGrid Grid { get; } = grid;

    public MosaicPlan Plan { get; } = plan;

    public PixelBuffer Image { get; } = image;

    public string Summary => TF(
        Texts.Build.Summary,
        Plan.Columns, Plan.Rows, Image.Width, Image.Height, Plan.UniqueTiles, Library.Tiles.Count, Plan.MeanError);
}

/// <summary>The whole pipeline: load tiles → load target → match → render.</summary>
public sealed class MosaicBuilder(string? cacheDirectory, Action<string>? log = null)
{
    public MosaicBuilder()
        : this(TileCache.DefaultDirectory)
    {
    }

    public MosaicResult Build(
        MosaicSettings settings,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var errors = settings.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, errors)); // no parameter name: the message is shown to users
        }

        var total = Stopwatch.StartNew();
        var info = ImageCodec.ReadInfo(settings.TargetImagePath);
        var (outWidth, outHeight, columns, rows) = settings.OutputSize(info.Width, info.Height);
        if ((long)outWidth * outHeight > MosaicSettings.MaxOutputPixels)
        {
            throw new ArgumentException(TF(
                Texts.Error.TooBig,
                outWidth, outHeight, MosaicSettings.MaxOutputPixels / 1_000_000));
        }

        var library = LoadLibrary(settings, progress, cancellationToken);

        var step = Stopwatch.StartNew();
        progress?.Report(new MosaicProgress(MosaicStage.LoadingTarget, 0, 1));
        // Decode only as much resolution as the output can show.
        var target = ImageCodec.Load(settings.TargetImagePath, minShortSide: Math.Min(outWidth, outHeight));
        var grid = TargetGrid.FromImage(target, columns, rows, settings.FeatureGrid);
        log?.Invoke(TF(Texts.Log.Target, info.Width, info.Height, columns, rows, step.Elapsed.TotalSeconds));

        step.Restart();
        var plan = TileMatcher.Match(grid, library, settings.ToMatchOptions(), progress, cancellationToken);
        if (plan.RepeatDistance < settings.MinRepeatDistance)
        {
            log?.Invoke(TF(Texts.Log.RepeatLowered, library.Tiles.Count, plan.RepeatDistance));
        }

        log?.Invoke(TF(Texts.Log.Matching, step.Elapsed.TotalSeconds));

        step.Restart();
        var image = MosaicRenderer.Render(plan, library, grid, target, settings.ToRenderOptions(), progress, cancellationToken);
        log?.Invoke(TF(Texts.Log.Rendering, step.Elapsed.TotalSeconds));

        var result = new MosaicResult(settings, library, target, grid, plan, image);
        log?.Invoke(TF(Texts.Log.Done, total.Elapsed.TotalSeconds, result.Summary));
        progress?.Report(new MosaicProgress(MosaicStage.Done, 1, 1));
        return result;
    }

    /// <summary>Loads the tiles of the settings' folders (from the cache where possible).</summary>
    public TileLibrary LoadLibrary(
        MosaicSettings settings,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var errors = settings.ValidateTilesFolders();
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, errors)); // no parameter name: the message is shown to users
        }

        var folders = settings.TilesFolders.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
        var libraryOptions = new TileLibraryOptions(folders, settings.TileSize, settings.FeatureGrid)
        {
            CacheDirectory = cacheDirectory,
        };
        var library = TileLibrary.Load(libraryOptions, progress, log, cancellationToken);
        log?.Invoke(TF(Texts.Log.Tiles, library.Stats));
        if (library.Tiles.Count == 0)
        {
            throw new InvalidOperationException(TF(Texts.Error.NoImages, string.Join(", ", folders)));
        }

        return library;
    }

    /// <summary>Draws an existing plan again with new color correction / overlay values (no reloading or matching).</summary>
    public static MosaicResult Rerender(
        MosaicResult previous,
        double colorCorrection,
        double overlay,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Rerender(previous, new RenderOptions { ColorCorrection = colorCorrection, Overlay = overlay }, progress, cancellationToken);

    /// <summary>Draws an existing plan again with new rendering options (no reloading or matching).</summary>
    public static MosaicResult Rerender(
        MosaicResult previous,
        RenderOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var settings = previous.Settings with
        {
            ColorCorrection = options.ColorCorrection,
            Overlay = options.Overlay,
            EdgeBlend = options.EdgeBlend,
        };
        var image = MosaicRenderer.Render(
            previous.Plan, previous.Library, previous.Grid, previous.Target, settings.ToRenderOptions(), progress, cancellationToken);
        return new MosaicResult(settings, previous.Library, previous.Target, previous.Grid, previous.Plan, image);
    }

    /// <summary>
    /// Renders a rectangle of whole cells of a finished mosaic with bigger tiles: the photos used there are read
    /// again at <paramref name="tileSize"/>, so an enlarged fragment shows the photos sharply instead of
    /// enlarged pixels. Same plan and rendering options as the mosaic. With a <paramref name="cache"/>, photos
    /// already read for an earlier fragment are not read again.
    /// </summary>
    public static PixelBuffer RenderFragment(
        MosaicResult result,
        CellRect fragment,
        int tileSize,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default,
        FragmentTileCache? cache = null)
    {
        var plan = result.Plan;
        if (fragment.Column < 0 || fragment.Row < 0 || fragment.Columns < 1 || fragment.Rows < 1 ||
            fragment.Column + fragment.Columns > plan.Columns || fragment.Row + fragment.Rows > plan.Rows)
        {
            throw new ArgumentOutOfRangeException(nameof(fragment), "The fragment must lie inside the mosaic.");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(tileSize, 1);

        // Render one extra cell around the fragment where the mosaic has one, so that tile edges and the overlay
        // at the fragment's border look exactly as in the whole mosaic; the margin is cut off afterwards.
        var padded = new CellRect(
            Math.Max(0, fragment.Column - 1),
            Math.Max(0, fragment.Row - 1),
            0,
            0);
        padded = padded with
        {
            Columns = Math.Min(plan.Columns, fragment.Column + fragment.Columns + 1) - padded.Column,
            Rows = Math.Min(plan.Rows, fragment.Row + fragment.Rows + 1) - padded.Row,
        };

        // The plan of the padded fragment, with its own small library of the photos it uses.
        var indices = new int[padded.Columns * padded.Rows];
        var errors = new float[indices.Length];
        var remap = new Dictionary<int, int>();
        var sources = new List<Tile>();
        for (var r = 0; r < padded.Rows; r++)
        {
            for (var c = 0; c < padded.Columns; c++)
            {
                var cell = (padded.Row + r) * plan.Columns + padded.Column + c;
                var original = plan.TileIndices[cell];
                if (!remap.TryGetValue(original, out var local))
                {
                    remap[original] = local = sources.Count;
                    sources.Add(result.Library.Tiles[original]);
                }

                indices[r * padded.Columns + c] = local;
                errors[r * padded.Columns + c] = plan.CellErrors[cell];
            }
        }

        var tiles = new Tile[sources.Count];
        var parallel = new ParallelOptions { CancellationToken = cancellationToken };
        Parallel.For(0, sources.Count, parallel, i =>
        {
            var source = sources[i];
            PixelBuffer pixels;
            try
            {
                pixels = tileSize == result.Library.TileSize ? source.Pixels
                    : cache != null ? cache.Get(source.SourcePath, tileSize)
                    : TileLibrary.PrepareTile(source.SourcePath, tileSize);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // The photo moved or became unreadable since the build: enlarge the prepared tile instead.
                pixels = source.Pixels.Resize(tileSize, tileSize);
            }

            tiles[i] = new Tile(source.SourcePath, pixels, result.Library.FeatureGrid);
        });

        var library = new TileLibrary(result.Library.Options with { TileSize = tileSize }, tiles, result.Library.Stats);
        var subPlan = new MosaicPlan(padded.Columns, padded.Rows, indices, errors, plan.RepeatDistance);
        var grid = result.Grid.Region(padded);

        // The exact part of the target under the padded fragment, in target pixels, for the overlay.
        var target = result.Target;
        var targetRegion = new TargetRegion(
            (double)padded.Column * target.Width / plan.Columns,
            (double)padded.Row * target.Height / plan.Rows,
            (double)padded.Columns * target.Width / plan.Columns,
            (double)padded.Rows * target.Height / plan.Rows);

        var image = MosaicRenderer.Render(subPlan, library, grid, target, result.Settings.ToRenderOptions(), progress, cancellationToken, targetRegion);
        return image.Crop(
            (fragment.Column - padded.Column) * tileSize,
            (fragment.Row - padded.Row) * tileSize,
            fragment.Columns * tileSize,
            fragment.Rows * tileSize);
    }
}

/// <summary>A rectangle of whole cells: first column and row, and how many columns and rows.</summary>
public readonly record struct CellRect(int Column, int Row, int Columns, int Rows);
