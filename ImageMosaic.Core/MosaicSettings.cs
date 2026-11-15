using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Mosaic;

namespace ImageMosaic.Core;

/// <summary>Everything needed to build a mosaic.</summary>
public sealed record MosaicSettings
{
    public const int MinColumns = 4;
    public const int MaxColumns = 500;
    public const int MinTileSize = 8;
    public const int MaxTileSize = 256;
    public const long MaxOutputPixels = 250_000_000;

    /// <summary>The picture the mosaic reproduces.</summary>
    public string TargetImagePath { get; init; } = "";

    /// <summary>Folder (searched recursively) with the photos used as tiles.</summary>
    public string TilesFolder { get; init; } = "";

    /// <summary>Number of tiles across; the number of rows follows from the target's aspect ratio.</summary>
    public int Columns { get; init; } = 80;

    /// <summary>Size of one square tile in the output, in pixels.</summary>
    public int TileSize { get; init; } = 32;

    /// <summary>Each tile and cell is compared as a FeatureGrid × FeatureGrid grid of average colors.</summary>
    public int FeatureGrid { get; init; } = 3;

    public int MinRepeatDistance { get; init; } = 4;

    public double UsagePenalty { get; init; } = 3;

    public double ColorCorrection { get; init; } = 0.4;

    public double Overlay { get; init; } = 0.2;

    public int Seed { get; init; } = 1;

    public MatchOptions ToMatchOptions() => new()
    {
        MinRepeatDistance = MinRepeatDistance,
        UsagePenalty = UsagePenalty,
        Seed = Seed,
    };

    public RenderOptions ToRenderOptions() => new()
    {
        ColorCorrection = ColorCorrection,
        Overlay = Overlay,
    };

    /// <summary>Checks the settings and the paths they refer to. Returns human-readable problems; empty when valid.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(TargetImagePath))
        {
            errors.Add("Choose the target image.");
        }
        else if (!File.Exists(TargetImagePath))
        {
            errors.Add($"Target image not found: {TargetImagePath}");
        }
        else if (!ImageCodec.IsSupported(TargetImagePath))
        {
            errors.Add($"Unsupported image type: {Path.GetExtension(TargetImagePath)}");
        }

        if (string.IsNullOrWhiteSpace(TilesFolder))
        {
            errors.Add("Choose the folder with tile photos.");
        }
        else if (!Directory.Exists(TilesFolder))
        {
            errors.Add($"Tiles folder not found: {TilesFolder}");
        }

        if (Columns is < MinColumns or > MaxColumns)
        {
            errors.Add($"Columns must be between {MinColumns} and {MaxColumns}.");
        }

        if (TileSize is < MinTileSize or > MaxTileSize)
        {
            errors.Add($"Tile size must be between {MinTileSize} and {MaxTileSize} px.");
        }

        if (FeatureGrid is < 1 or > 8)
        {
            errors.Add("Feature grid must be between 1 and 8.");
        }

        if (MinRepeatDistance < 0)
        {
            errors.Add("Repeat distance cannot be negative.");
        }

        if (UsagePenalty < 0)
        {
            errors.Add("Usage penalty cannot be negative.");
        }

        if (ColorCorrection is < 0 or > 1 || Overlay is < 0 or > 1)
        {
            errors.Add("Color correction and overlay must be between 0 and 1.");
        }

        return errors;
    }

    /// <summary>Output size in pixels for a target of the given (displayed) size.</summary>
    public (int Width, int Height, int Rows) OutputSize(int targetWidth, int targetHeight)
    {
        var rows = TargetGrid.RowsFor(Columns, targetWidth, targetHeight);
        return (Columns * TileSize, rows * TileSize, rows);
    }
}
