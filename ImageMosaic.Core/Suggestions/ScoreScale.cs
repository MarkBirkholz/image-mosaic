namespace ImageMosaic.Core.Suggestions;

/// <summary>Maps a raw measurement to a 0–1 mark: <see cref="Worst"/> gives 0, <see cref="Best"/> gives 1, linear between.</summary>
public readonly record struct MarkRange(double Worst, double Best)
{
    public double Mark(double value) =>
        Best == Worst ? 1 : Math.Clamp((value - Worst) / (Best - Worst), 0, 1);
}

public readonly record struct ScoreParts(double ColorMatch, double ColorVariety, double Contrast, double Detail);

/// <summary>Turns measurements into marks, either with fixed thresholds or calibrated to a set of photos.</summary>
public sealed record ScoreScale(MarkRange ColorError, MarkRange ColorVariety, MarkRange Contrast, MarkRange Detail)
{
    // Below this many photos the spread of a library says little: fixed thresholds are used.
    private const int MinCandidatesForCalibration = 20;

    /// <summary>
    /// Fixed thresholds: color difference ΔE 22 → 0 … 6 → full; one hue → 0 … all hues → full;
    /// contrast 0 → 0 … 20 → full; detail ΔE 0 → 0 … 9 → full.
    /// </summary>
    public static ScoreScale Absolute { get; } = new(
        ColorError: new MarkRange(22, 6),
        ColorVariety: new MarkRange(0, 1),
        Contrast: new MarkRange(0, 20),
        Detail: new MarkRange(0, 9));

    /// <summary>Per criterion: 2nd percentile of the photos → 0, 98th → 1, with a minimum range width.</summary>
    public static ScoreScale FromCandidates(IReadOnlyCollection<TargetMetrics> metrics)
    {
        if (metrics.Count < MinCandidatesForCalibration)
        {
            return Absolute;
        }

        return new ScoreScale(
            ColorError: Calibrate(metrics.Select(m => m.MeanError), lowerIsBetter: true, minWidth: 4),
            ColorVariety: Calibrate(metrics.Select(m => m.ColorVariety), lowerIsBetter: false, minWidth: 0.15),
            Contrast: Calibrate(metrics.Select(m => m.Contrast), lowerIsBetter: false, minWidth: 4),
            Detail: Calibrate(metrics.Select(m => m.Detail), lowerIsBetter: false, minWidth: 1.5));
    }

    public ScoreParts Parts(TargetMetrics m) => new(
        ColorMatch: ColorError.Mark(m.MeanError),
        ColorVariety: ColorVariety.Mark(m.ColorVariety),
        Contrast: Contrast.Mark(m.Contrast),
        Detail: Detail.Mark(m.Detail));

    /// <summary>Value below which the given share (0–1) of the values lie, interpolated.</summary>
    public static double Percentile(IEnumerable<double> values, double share)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0)
        {
            return 0;
        }

        var position = Math.Clamp(share, 0, 1) * (sorted.Length - 1);
        var lower = (int)Math.Floor(position);
        var upper = Math.Min(lower + 1, sorted.Length - 1);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }

    private static MarkRange Calibrate(IEnumerable<double> values, bool lowerIsBetter, double minWidth)
    {
        var list = values.ToList();
        var low = Percentile(list, 0.02);
        var high = Percentile(list, 0.98);
        if (high - low < minWidth)
        {
            var center = (low + high) / 2;
            low = center - minWidth / 2;
            high = center + minWidth / 2;
        }

        return lowerIsBetter ? new MarkRange(high, low) : new MarkRange(low, high);
    }
}

/// <summary>Short reasons shown with a suggestion.</summary>
public enum SuggestionNote
{
    ColorsWellCovered,
    FewPhotosInTheseColors,
    ClearShapes,
    LowContrast,
    Colorful,
    MostlyOneColor,
    LotsOfDetail,
    LittleDetail,
}
