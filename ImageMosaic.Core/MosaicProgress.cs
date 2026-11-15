namespace ImageMosaic.Core;

public enum MosaicStage
{
    Scanning,
    LoadingTiles,
    LoadingTarget,
    Matching,
    Rendering,
    Done,
}

/// <summary>Progress of a long operation: <paramref name="Done"/> of <paramref name="Total"/> items of the current stage.</summary>
public readonly record struct MosaicProgress(MosaicStage Stage, int Done, int Total)
{
    public double Fraction => Total <= 0 ? 0 : Math.Clamp((double)Done / Total, 0, 1);

    public override string ToString() => Stage switch
    {
        MosaicStage.Scanning => "Scanning folder…",
        MosaicStage.LoadingTiles => $"Loading tiles {Done:N0} / {Total:N0}",
        MosaicStage.LoadingTarget => "Loading target image…",
        MosaicStage.Matching => $"Matching {Done:N0} / {Total:N0} cells",
        MosaicStage.Rendering => $"Rendering {Done:N0} / {Total:N0} rows",
        _ => "Done",
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
