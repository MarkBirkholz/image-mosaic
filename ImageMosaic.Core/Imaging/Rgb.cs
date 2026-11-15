namespace ImageMosaic.Core.Imaging;

/// <summary>An 8-bit sRGB color.</summary>
public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static Rgb FromFloat(float r, float g, float b) => new(ClampToByte(r), ClampToByte(g), ClampToByte(b));

    public static byte ClampToByte(float value)
    {
        if (value <= 0f)
        {
            return 0;
        }

        if (value >= 255f)
        {
            return 255;
        }

        return (byte)(value + 0.5f);
    }

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}
