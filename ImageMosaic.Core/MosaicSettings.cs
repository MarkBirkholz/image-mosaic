using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Mosaic;
using ImageMosaic.Core.Suggestions;

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

    /// <summary>Folders (each searched recursively) with the photos used as tiles.</summary>
    public IReadOnlyList<string> TilesFolders { get; init; } = [];

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

    /// <summary>0–1: soft transition between neighbouring tiles (0 = sharp edges).</summary>
    public double EdgeBlend { get; init; }

    public int Seed { get; init; } = 1;

    /// <summary>Weights for ranking suggested targets (not used when building).</summary>
    public SuggestionWeights SuggestionWeights { get; init; } = SuggestionWeights.Default;

    /// <summary>Interface language: "en", "ru", or null for the system language (not used when building).</summary>
    public string? Language { get; init; }

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
        EdgeBlend = EdgeBlend,
    };

    /// <summary>Checks the settings and the paths they refer to. Returns human-readable problems; empty when valid.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(TargetImagePath))
        {
            errors.Add(T(Texts.Validation.ChooseTarget));
        }
        else if (!File.Exists(TargetImagePath))
        {
            errors.Add(TF(Texts.Validation.TargetNotFound, TargetImagePath));
        }
        else if (!ImageCodec.IsSupported(TargetImagePath))
        {
            errors.Add(TF(Texts.Validation.UnsupportedType, Path.GetExtension(TargetImagePath)));
        }

        errors.AddRange(ValidateTilesFolders());

        if (Columns is < MinColumns or > MaxColumns)
        {
            errors.Add(TF(Texts.Validation.ColumnsRange, MinColumns, MaxColumns));
        }

        if (TileSize is < MinTileSize or > MaxTileSize)
        {
            errors.Add(TF(Texts.Validation.TileSizeRange, MinTileSize, MaxTileSize));
        }

        if (FeatureGrid is < 1 or > 8)
        {
            errors.Add(T(Texts.Validation.GridRange));
        }

        if (MinRepeatDistance < 0)
        {
            errors.Add(T(Texts.Validation.RepeatNegative));
        }

        if (UsagePenalty < 0)
        {
            errors.Add(T(Texts.Validation.VarietyNegative));
        }

        if (ColorCorrection is < 0 or > 1 || Overlay is < 0 or > 1 || EdgeBlend is < 0 or > 1)
        {
            errors.Add(T(Texts.Validation.ColorsRange));
        }

        return errors;
    }

    /// <summary>Checks only the tile folders (enough for loading a tile library).</summary>
    public IReadOnlyList<string> ValidateTilesFolders()
    {
        var errors = new List<string>();
        var folders = TilesFolders.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
        if (folders.Count == 0)
        {
            errors.Add(T(Texts.Validation.AddFolder));
        }

        errors.AddRange(folders.Where(f => !Directory.Exists(f)).Select(f => TF(Texts.Validation.FolderNotFound, f)));
        return errors;
    }

    /// <summary>Output size in pixels for a target of the given (displayed) size.</summary>
    public (int Width, int Height, int Rows) OutputSize(int targetWidth, int targetHeight)
    {
        var rows = TargetGrid.RowsFor(Columns, targetWidth, targetHeight);
        return (Columns * TileSize, rows * TileSize, rows);
    }
}
