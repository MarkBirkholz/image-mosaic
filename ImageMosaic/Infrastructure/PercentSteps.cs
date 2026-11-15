namespace ImageMosaic.Infrastructure;

/// <summary>
/// Percent sliders move in fixed 5 % steps: the track bar holds the step number (0–20),
/// so dragging can only land on 0, 5, 10 … 100 %.
/// </summary>
internal static class PercentSteps
{
    public const int Step = 5;
    public const int Count = 100 / Step;

    public static int ToSteps(double fraction) => Math.Clamp((int)Math.Round(fraction * 100 / Step), 0, Count);

    public static int ToSteps(int percent) => Math.Clamp((int)Math.Round(percent / (double)Step), 0, Count);

    public static double ToFraction(int steps) => steps * Step / 100.0;

    public static int ToPercent(int steps) => steps * Step;
}
