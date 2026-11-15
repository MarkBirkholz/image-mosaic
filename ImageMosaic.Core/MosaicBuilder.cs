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

    public string Summary =>
        $"{Plan.Columns}×{Plan.Rows} tiles, {Image.Width:N0}×{Image.Height:N0} px, " +
        $"{Plan.UniqueTiles:N0} of {Library.Tiles.Count:N0} photos used, mean color error ΔE {Plan.MeanError:N1}";
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
            throw new ArgumentException(string.Join(Environment.NewLine, errors), nameof(settings));
        }

        var total = Stopwatch.StartNew();
        var info = ImageCodec.ReadInfo(settings.TargetImagePath);
        var (outWidth, outHeight, rows) = settings.OutputSize(info.Width, info.Height);
        if ((long)outWidth * outHeight > MosaicSettings.MaxOutputPixels)
        {
            throw new ArgumentException(
                $"The mosaic would be {outWidth:N0}×{outHeight:N0} px, more than {MosaicSettings.MaxOutputPixels / 1_000_000} megapixels. " +
                "Use fewer columns or smaller tiles.");
        }

        var libraryOptions = new TileLibraryOptions(settings.TilesFolder, settings.TileSize, settings.FeatureGrid)
        {
            CacheDirectory = cacheDirectory,
        };
        var library = TileLibrary.Load(libraryOptions, progress, log, cancellationToken);
        log?.Invoke($"Tiles: {library.Stats}");
        if (library.Tiles.Count == 0)
        {
            throw new InvalidOperationException($"No readable images found in {settings.TilesFolder}.");
        }

        var step = Stopwatch.StartNew();
        progress?.Report(new MosaicProgress(MosaicStage.LoadingTarget, 0, 1));
        // Decode only as much resolution as the output can show.
        var target = ImageCodec.Load(settings.TargetImagePath, minShortSide: Math.Min(outWidth, outHeight));
        var grid = TargetGrid.FromImage(target, settings.Columns, rows, settings.FeatureGrid);
        log?.Invoke($"Target: {info.Width}×{info.Height} px → {settings.Columns}×{rows} cells ({step.Elapsed.TotalSeconds:N1} s)");

        step.Restart();
        var plan = TileMatcher.Match(grid, library, settings.ToMatchOptions(), progress, cancellationToken);
        if (plan.RepeatDistance < settings.MinRepeatDistance)
        {
            log?.Invoke($"Only {library.Tiles.Count} photos: repeat distance lowered to {plan.RepeatDistance}.");
        }

        log?.Invoke($"Matching: {step.Elapsed.TotalSeconds:N1} s");

        step.Restart();
        var image = MosaicRenderer.Render(plan, library, grid, target, settings.ToRenderOptions(), progress, cancellationToken);
        log?.Invoke($"Rendering: {step.Elapsed.TotalSeconds:N1} s");

        var result = new MosaicResult(settings, library, target, grid, plan, image);
        log?.Invoke($"Done in {total.Elapsed.TotalSeconds:N1} s: {result.Summary}");
        progress?.Report(new MosaicProgress(MosaicStage.Done, 1, 1));
        return result;
    }

    /// <summary>Draws an existing plan again with new color correction / overlay values (no reloading or matching).</summary>
    public static MosaicResult Rerender(
        MosaicResult previous,
        double colorCorrection,
        double overlay,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var settings = previous.Settings with { ColorCorrection = colorCorrection, Overlay = overlay };
        var image = MosaicRenderer.Render(
            previous.Plan, previous.Library, previous.Grid, previous.Target, settings.ToRenderOptions(), progress, cancellationToken);
        return new MosaicResult(settings, previous.Library, previous.Target, previous.Grid, previous.Plan, image);
    }
}
