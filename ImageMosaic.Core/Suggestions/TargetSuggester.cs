using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Mosaic;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Core.Suggestions;

/// <summary>Criterion weights, 0–100; only their ratios matter.</summary>
public sealed record SuggestionWeights
{
    public static SuggestionWeights Default { get; } = new();

    public double ColorMatch { get; init; } = 40;

    public double ColorVariety { get; init; } = 25;

    public double Contrast { get; init; } = 20;

    /// <summary>−100…100: negative prefers simpler photos, positive more detailed ones, 0 ignores detail.</summary>
    public double Detail { get; init; }

    /// <summary>Nothing chosen: color match, color variety and contrast then count equally.</summary>
    [JsonIgnore]
    public bool AllZero => ColorMatch <= 0 && ColorVariety <= 0 && Contrast <= 0 && Detail == 0;
}

public sealed record SuggestionOptions
{
    /// <summary>Columns of the trial mosaic each candidate is measured with.</summary>
    public int Columns { get; init; } = 40;

    /// <summary>Upper limit of photos analyzed; larger libraries are sampled evenly.</summary>
    public int MaxCandidates { get; init; } = 1500;

    public int Count { get; init; } = 24;

    public MatchOptions Match { get; init; } = new();

    public SuggestionWeights Weights { get; init; } = SuggestionWeights.Default;
}

/// <summary>Measurements of a trial mosaic of a candidate photo.</summary>
/// <param name="MeanError">Mean color difference (ΔE) between the picture and its trial mosaic.</param>
/// <param name="Contrast">Spread of lightness between cells (standard deviation of L*).</param>
/// <param name="Detail">Average color difference (ΔE) between the parts of a cell and the cell's average color.</param>
/// <param name="ColorVariety">0–1 entropy of the cells' hues: 0 = one color, 1 = all hues equally.</param>
public sealed record TargetMetrics(double MeanError, double Contrast, double Detail, double ColorVariety);

/// <summary>An analyzed photo; ranking it again needs no image work.</summary>
public sealed class TargetCandidate(string path, int width, int height, TargetMetrics metrics)
{
    public string Path { get; } = path;

    public int Width { get; } = width;

    public int Height { get; } = height;

    public TargetMetrics Metrics { get; } = metrics;
}

public sealed class TargetSuggestion(TargetCandidate candidate, double score, IReadOnlyList<SuggestionNote> notes, ScoreParts parts)
{
    public TargetCandidate Candidate { get; } = candidate;

    public string Path => Candidate.Path;

    public int Width => Candidate.Width;

    public int Height => Candidate.Height;

    public TargetMetrics Metrics => Candidate.Metrics;

    public double Score { get; } = score;

    public IReadOnlyList<SuggestionNote> Notes { get; } = notes;

    public ScoreParts Parts { get; } = parts;
}

/// <summary>Ranks photos as mosaic targets by color statistics only.</summary>
public static class TargetSuggester
{
    public const int ThumbnailShortSide = 128;

    public static IReadOnlyList<TargetSuggestion> Suggest(
        TileLibrary library,
        SuggestionOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Suggest(library, library.Tiles.Select(t => t.SourcePath).ToList(), options, progress, cancellationToken);

    public static IReadOnlyList<TargetSuggestion> Suggest(
        TileLibrary library,
        IReadOnlyList<string> candidates,
        SuggestionOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        Rank(Analyze(library, candidates, options, progress, cancellationToken), options.Weights, options.Count);

    /// <summary>Slow: decodes each photo small and measures a trial mosaic of it.</summary>
    public static IReadOnlyList<TargetCandidate> Analyze(
        TileLibrary library,
        IReadOnlyList<string> candidates,
        SuggestionOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sample = Sample(candidates, options.MaxCandidates);
        var results = new ConcurrentBag<TargetCandidate>();
        var counter = new ThrottledProgress(progress, MosaicStage.Analyzing, sample.Count);
        counter.Start();

        var parallel = new ParallelOptions { CancellationToken = cancellationToken };
        Parallel.ForEach(sample, parallel, path =>
        {
            try
            {
                var info = ImageCodec.ReadInfo(path);
                var thumbnail = ImageCodec.Load(path, ThumbnailShortSide);
                results.Add(new TargetCandidate(path, info.Width, info.Height, Measure(thumbnail, library, options)));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
            }

            counter.Increment();
        });

        return results.OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Fast: scores photos with marks calibrated to them, best first.</summary>
    public static IReadOnlyList<TargetSuggestion> Rank(IEnumerable<TargetCandidate> candidates, SuggestionWeights weights, int count)
    {
        var list = candidates.ToList();
        var scale = ScoreScale.FromCandidates(list.Select(c => c.Metrics).ToList());
        return list
            .Select(c =>
            {
                var (score, notes) = Score(c.Metrics, weights, scale);
                return new TargetSuggestion(c, score, notes, scale.Parts(c.Metrics));
            })
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Path, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToList();
    }

    public static TargetMetrics Measure(PixelBuffer image, TileLibrary library, SuggestionOptions options)
    {
        var columns = options.Columns;
        var rows = TargetGrid.RowsFor(columns, image.Width, image.Height);
        var grid = TargetGrid.FromImage(image, columns, rows, library.FeatureGrid);
        var plan = TileMatcher.Match(grid, library, options.Match);

        var lightness = grid.MeansLab.Select(c => (double)c.L).ToArray();
        var meanL = lightness.Average();
        var contrast = Math.Sqrt(lightness.Average(l => (l - meanL) * (l - meanL)));

        // Detail: distance of each cell's parts from the cell's average color.
        var n = grid.FeatureGrid * grid.FeatureGrid;
        var detailSum = 0.0;
        for (var cell = 0; cell < grid.CellCount; cell++)
        {
            var mean = grid.MeansLab[cell];
            var squares = 0f;
            foreach (var feature in grid.CellFeatures(cell))
            {
                squares += ColorMath.DistanceSquared(feature, mean);
            }

            detailSum += MathF.Sqrt(squares / n);
        }

        return new TargetMetrics(
            MeanError: plan.MeanError,
            Contrast: contrast,
            Detail: detailSum / grid.CellCount,
            ColorVariety: HueEntropy(grid.MeansLab));
    }

    public static (double Score, IReadOnlyList<SuggestionNote> Notes) Score(TargetMetrics m) =>
        Score(m, SuggestionWeights.Default, ScoreScale.Absolute);

    /// <summary>Weighted average of the four marks, 0–100.</summary>
    public static (double Score, IReadOnlyList<SuggestionNote> Notes) Score(TargetMetrics m, SuggestionWeights weights, ScoreScale scale)
    {
        var p = scale.Parts(m);
        if (weights.AllZero)
        {
            weights = new SuggestionWeights { ColorMatch = 1, ColorVariety = 1, Contrast = 1, Detail = 0 };
        }

        var marks = new (double Weight, double Mark)[]
        {
            (weights.ColorMatch, p.ColorMatch),
            (weights.ColorVariety, p.ColorVariety),
            (weights.Contrast, p.Contrast),
            // Negative detail weight rewards simpler photos.
            (Math.Abs(weights.Detail), weights.Detail >= 0 ? p.Detail : 1 - p.Detail),
        };
        var totalWeight = marks.Sum(x => Math.Max(0, x.Weight));
        var quality = marks.Sum(x => Math.Max(0, x.Weight) * x.Mark) / totalWeight;

        var notes = new List<SuggestionNote>();
        AddNote(notes, p.ColorMatch, SuggestionNote.ColorsWellCovered, SuggestionNote.FewPhotosInTheseColors);
        AddNote(notes, p.ColorVariety, SuggestionNote.Colorful, SuggestionNote.MostlyOneColor);
        AddNote(notes, p.Contrast, SuggestionNote.ClearShapes, SuggestionNote.LowContrast);
        AddNote(notes, p.Detail, SuggestionNote.LotsOfDetail, SuggestionNote.LittleDetail);
        return (Math.Round(100 * quality, 1), notes);
    }

    private static void AddNote(List<SuggestionNote> notes, double mark, SuggestionNote high, SuggestionNote low)
    {
        if (mark >= 0.75)
        {
            notes.Add(high);
        }
        else if (mark < 0.25)
        {
            notes.Add(low);
        }
    }

    /// <summary>Normalized entropy of 12 hue sectors plus one bin for nearly gray cells.</summary>
    private static double HueEntropy(Lab[] cells)
    {
        const int hueBins = 12;
        var counts = new int[hueBins + 1];
        foreach (var c in cells)
        {
            var chroma = MathF.Sqrt(c.A * c.A + c.B * c.B);
            if (chroma < 8)
            {
                counts[hueBins]++;
            }
            else
            {
                var hue = Math.Atan2(c.B, c.A); // -π … π
                var bin = (int)((hue + Math.PI) / (2 * Math.PI) * hueBins) % hueBins;
                counts[bin]++;
            }
        }

        var entropy = 0.0;
        foreach (var count in counts.Where(x => x > 0))
        {
            var p = (double)count / cells.Length;
            entropy -= p * Math.Log(p);
        }

        return entropy / Math.Log(counts.Length);
    }

    private static IReadOnlyList<string> Sample(IReadOnlyList<string> candidates, int max)
    {
        if (candidates.Count <= max)
        {
            return candidates;
        }

        var step = (double)candidates.Count / max;
        return Enumerable.Range(0, max).Select(i => candidates[(int)(i * step)]).ToList();
    }
}
