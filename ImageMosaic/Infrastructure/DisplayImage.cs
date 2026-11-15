using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ImageMosaic.Core.Imaging;

namespace ImageMosaic.Infrastructure;

/// <summary>A picture prepared for display: the full image plus, for large images, a downscaled copy for zoomed-out views.</summary>
internal sealed class DisplayImage : IDisposable
{
    private const int PreviewMaxSide = 2400;

    private DisplayImage(Bitmap full, Bitmap? preview)
    {
        Full = full;
        Preview = preview;
    }

    public Bitmap Full { get; }

    public Bitmap? Preview { get; }

    public Size Size => Full.Size;

    /// <summary>Converts in the calling thread; call from a background task for large images.</summary>
    public static DisplayImage From(PixelBuffer buffer)
    {
        Bitmap? preview = null;
        var longSide = Math.Max(buffer.Width, buffer.Height);
        if (longSide > PreviewMaxSide)
        {
            var scale = (double)PreviewMaxSide / longSide;
            var small = buffer.Resize(
                Math.Max(1, (int)Math.Round(buffer.Width * scale)),
                Math.Max(1, (int)Math.Round(buffer.Height * scale)));
            preview = ToBitmap(small);
        }

        return new DisplayImage(ToBitmap(buffer), preview);
    }

    public static Bitmap ToBitmap(PixelBuffer buffer)
    {
        var bitmap = new Bitmap(buffer.Width, buffer.Height, PixelFormat.Format24bppRgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, buffer.Width, buffer.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            for (var y = 0; y < buffer.Height; y++)
            {
                Marshal.Copy(buffer.Data, y * buffer.Stride, data.Scan0 + y * data.Stride, buffer.Stride);
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        return bitmap;
    }

    public void Dispose()
    {
        Full.Dispose();
        Preview?.Dispose();
    }
}
