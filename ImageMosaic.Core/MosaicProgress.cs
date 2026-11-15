namespace ImageMosaic.Core;

public enum MosaicStage
{
    Scanning,
    LoadingTiles,
    LoadingTarget,
    Matching,
    Rendering,
    Analyzing,
    Done,
}

/// <summary>Progress of a long operation: <paramref name="Done"/> of <paramref name="Total"/> items of the current stage.</summary>
public readonly record struct MosaicProgress(MosaicStage Stage, int Done, int Total)
{
    public double Fraction => Total <= 0 ? 0 : Math.Clamp((double)Done / Total, 0, 1);

    public override string ToString() => Stage switch
    {
        MosaicStage.Scanning => T(Texts.Progress.Scanning),
        MosaicStage.LoadingTiles => TF(Texts.Progress.LoadingTiles, Done, Total),
        MosaicStage.LoadingTarget => T(Texts.Progress.LoadingTarget),
        MosaicStage.Matching => TF(Texts.Progress.Matching, Done, Total),
        MosaicStage.Rendering => TF(Texts.Progress.Rendering, Done, Total),
        MosaicStage.Analyzing => TF(Texts.Progress.Analyzing, Done, Total),
        _ => T(Texts.Progress.Done),
    };
}

/// <summary>Reports progress at most ~200 times per stage so the UI thread is not flooded.</summary>
internal sealed class ThrottledProgress(IProgress<MosaicProgress>? inner, MosaicStage stage, int total)
{
    private readonly int step = Math.Max(1, total / 200);
    private int done;

    public void Start() => inner?.Report(new MosaicProgress(stage, 0, total));

    public void Increment()
    {
        var value = Interlocked.Increment(ref done);
        if (inner != null && (value % step == 0 || value == total))
        {
            inner.Report(new MosaicProgress(stage, value, total));
        }
    }
}
