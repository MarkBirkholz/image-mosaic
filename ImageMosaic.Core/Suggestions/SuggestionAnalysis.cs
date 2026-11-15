using System.Globalization;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Core.Suggestions;

/// <summary>Analyzed library photos, reusable while the library and the settings they depend on are unchanged.</summary>
public sealed class SuggestionAnalysis
{
    private SuggestionAnalysis(string key, IReadOnlyList<TargetCandidate> candidates)
    {
        Key = key;
        Candidates = candidates;
    }

    public string Key { get; }

    public IReadOnlyList<TargetCandidate> Candidates { get; }

    public static SuggestionAnalysis Run(
        TileLibrary library,
        SuggestionOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var paths = library.Tiles.Select(t => t.SourcePath).ToList();
        var candidates = TargetSuggester.Analyze(library, paths, options, progress, cancellationToken);
        return new SuggestionAnalysis(KeyFor(library, options), candidates);
    }

    /// <summary><paramref name="previous"/> if still valid, otherwise a new analysis.</summary>
    public static SuggestionAnalysis Reuse(
        SuggestionAnalysis? previous,
        TileLibrary library,
        SuggestionOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        previous != null && previous.IsValidFor(library, options)
            ? previous
            : Run(library, options, progress, cancellationToken);

    public bool IsValidFor(TileLibrary library, SuggestionOptions options) => Key == KeyFor(library, options);

    // Weights are not part of the key: they only affect ranking.
    public static string KeyFor(TileLibrary library, SuggestionOptions options)
    {
        // Paths and tile pixels, so edited photos change the key too.
        var content = new HashCode();
        foreach (var tile in library.Tiles)
        {
            content.Add(tile.SourcePath, StringComparer.OrdinalIgnoreCase);
            content.AddBytes(tile.Pixels.Data);
        }

        var m = options.Match;
        return string.Join(
            "|",
            library.Tiles.Count,
            content.ToHashCode().ToString("X8", CultureInfo.InvariantCulture),
            library.TileSize,
            library.FeatureGrid,
            options.Columns,
            options.MaxCandidates,
            m.MinRepeatDistance,
            m.UsagePenalty.ToString(CultureInfo.InvariantCulture),
            m.CandidateCount,
            m.Seed);
    }
}
