namespace ImageMosaic.Core.Imaging;

/// <summary>A color in CIE L*a*b* (D65). Euclidean distance approximates perceived difference (ΔE76).</summary>
public readonly record struct Lab(float L, float A, float B);

public static class ColorMath
{
    private static readonly float[] SrgbToLinearTable = BuildSrgbToLinearTable();

    // D65 reference white.
    private const float Xn = 0.95047f;
    private const float Yn = 1.00000f;
    private const float Zn = 1.08883f;

    private const float Epsilon = 216f / 24389f;
    private const float Kappa = 24389f / 27f;

    public static float SrgbToLinear(byte channel) => SrgbToLinearTable[channel];

    public static Lab ToLab(Rgb color) => ToLab(color.R, color.G, color.B);

    public static Lab ToLab(byte r, byte g, byte b)
    {
        var rl = SrgbToLinearTable[r];
        var gl = SrgbToLinearTable[g];
        var bl = SrgbToLinearTable[b];

        var x = (0.4124564f * rl + 0.3575761f * gl + 0.1804375f * bl) / Xn;
        var y = (0.2126729f * rl + 0.7151522f * gl + 0.0721750f * bl) / Yn;
        var z = (0.0193339f * rl + 0.1191920f * gl + 0.9503041f * bl) / Zn;

        var fx = F(x);
        var fy = F(y);
        var fz = F(z);

        return new Lab(116f * fy - 16f, 500f * (fx - fy), 200f * (fy - fz));
    }

    public static float DistanceSquared(in Lab a, in Lab b)
    {
        var dl = a.L - b.L;
        var da = a.A - b.A;
        var db = a.B - b.B;
        return dl * dl + da * da + db * db;
    }

    public static float Distance(in Lab a, in Lab b) => MathF.Sqrt(DistanceSquared(a, b));

    private static float F(float t) => t > Epsilon ? MathF.Cbrt(t) : (Kappa * t + 16f) / 116f;

    private static float[] BuildSrgbToLinearTable()
    {
        var table = new float[256];
        for (var i = 0; i < table.Length; i++)
        {
            var c = i / 255.0;
            table[i] = (float)(c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4));
        }

        return table;
    }
}
