using System.Numerics;

namespace ImageMosaic.Core.Imaging;

/// <summary>
/// A plain 24-bit image in memory: tightly packed rows, 3 bytes per pixel in B, G, R order
/// (the layout used by both WIC <c>Bgr24</c> and GDI+ <c>Format24bppRgb</c>).
/// All processing works on this type, so it is independent of any UI or imaging framework.
/// </summary>
public sealed class PixelBuffer
{
    public const int BytesPerPixel = 3;

    private const long ParallelThreshold = 256 * 1024;

    public PixelBuffer(int width, int height)
        : this(width, height, new byte[checked(width * height * BytesPerPixel)])
    {
    }

    public PixelBuffer(int width, int height, byte[] data)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length != (long)width * height * BytesPerPixel)
        {
            throw new ArgumentException($"Expected {width * height * BytesPerPixel} bytes, got {data.Length}.", nameof(data));
        }

        Width = width;
        Height = height;
        Data = data;
    }

    public int Width { get; }

    public int Height { get; }

    public int Stride => Width * BytesPerPixel;

    public byte[] Data { get; }

    public static PixelBuffer Solid(int width, int height, Rgb color)
    {
        var buffer = new PixelBuffer(width, height);
        var data = buffer.Data;
        for (var i = 0; i < data.Length; i += BytesPerPixel)
        {
            data[i] = color.B;
            data[i + 1] = color.G;
            data[i + 2] = color.R;
        }

        return buffer;
    }

    public Rgb GetPixel(int x, int y)
    {
        var i = Offset(x, y);
        return new Rgb(Data[i + 2], Data[i + 1], Data[i]);
    }

    public void SetPixel(int x, int y, Rgb color)
    {
        var i = Offset(x, y);
        Data[i] = color.B;
        Data[i + 1] = color.G;
        Data[i + 2] = color.R;
    }

    public PixelBuffer Clone() => new(Width, Height, (byte[])Data.Clone());

    /// <summary>Exact mean of all pixels (in sRGB values).</summary>
    public Rgb Average()
    {
        long b = 0, g = 0, r = 0;
        var data = Data;
        for (var i = 0; i < data.Length; i += BytesPerPixel)
        {
            b += data[i];
            g += data[i + 1];
            r += data[i + 2];
        }

        var count = (float)(Width * Height);
        return Rgb.FromFloat(r / count, g / count, b / count);
    }

    public PixelBuffer Crop(int x, int y, int width, int height)
    {
        if (x < 0 || y < 0 || width < 1 || height < 1 || x + width > Width || y + height > Height)
        {
            throw new ArgumentOutOfRangeException(nameof(width), $"Crop {x},{y} {width}x{height} is outside {Width}x{Height}.");
        }

        var result = new PixelBuffer(width, height);
        var rowBytes = width * BytesPerPixel;
        for (var row = 0; row < height; row++)
        {
            Buffer.BlockCopy(Data, Offset(x, y + row), result.Data, row * rowBytes, rowBytes);
        }

        return result;
    }

    /// <summary>Crops the largest centered region with the given width/height ratio.</summary>
    public PixelBuffer CropCenter(double aspectRatio)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(aspectRatio);
        var width = Width;
        var height = Height;
        if ((double)Width / Height > aspectRatio)
        {
            width = Math.Clamp((int)Math.Round(Height * aspectRatio), 1, Width);
        }
        else
        {
            height = Math.Clamp((int)Math.Round(Width / aspectRatio), 1, Height);
        }

        if (width == Width && height == Height)
        {
            return this;
        }

        return Crop((Width - width) / 2, (Height - height) / 2, width, height);
    }

    /// <summary>
    /// Resamples the image. Shrinking uses an exact area average (box filter) and enlarging uses
    /// bilinear interpolation, independently per axis.
    /// </summary>
    public PixelBuffer Resize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        if (width == Width && height == Height)
        {
            return Clone();
        }

        var cx = Contribution.Build(Width, width);
        var cy = Contribution.Build(Height, height);

        float[] result;
        // Do the pass that shrinks the most first, so the intermediate buffer is as small as possible.
        if ((long)width * Height <= (long)Width * height)
        {
            var tmp = HorizontalPass(Data, Width, Height, cx, width);
            result = VerticalPass(tmp, width, cy, height);
        }
        else
        {
            var tmp = VerticalPass(Data, Width, cy, height);
            result = HorizontalPass(tmp, Width, height, cx, width);
        }

        var output = new PixelBuffer(width, height);
        var outData = output.Data;
        for (var i = 0; i < result.Length; i++)
        {
            outData[i] = Rgb.ClampToByte(result[i]);
        }

        return output;
    }

    /// <summary>
    /// Returns the image as it should be displayed for an EXIF orientation value (1–8).
    /// Unknown values are treated as 1 (no change).
    /// </summary>
    public PixelBuffer ApplyExifOrientation(int orientation)
    {
        if (orientation is < 2 or > 8)
        {
            return this;
        }

        var swap = orientation >= 5;
        var w = swap ? Height : Width;
        var h = swap ? Width : Height;
        var result = new PixelBuffer(w, h);
        var src = Data;
        var dst = result.Data;
        var srcW = Width;
        var srcH = Height;

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var (sx, sy) = orientation switch
                {
                    2 => (srcW - 1 - x, y),
                    3 => (srcW - 1 - x, srcH - 1 - y),
                    4 => (x, srcH - 1 - y),
                    5 => (y, x),
                    6 => (y, srcH - 1 - x),
                    7 => (srcW - 1 - y, srcH - 1 - x),
                    _ => (srcW - 1 - y, x), // 8
                };
                var si = (sy * srcW + sx) * BytesPerPixel;
                var di = (y * w + x) * BytesPerPixel;
                dst[di] = src[si];
                dst[di + 1] = src[si + 1];
                dst[di + 2] = src[si + 2];
            }
        }

        return result;
    }

    private int Offset(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Pixel {x},{y} is outside {Width}x{Height}.");
        }

        return (y * Width + x) * BytesPerPixel;
    }

    private static float[] HorizontalPass<T>(T[] src, int srcWidth, int rows, Contribution[] cx, int dstWidth)
        where T : unmanaged, INumberBase<T>
    {
        var dst = new float[dstWidth * rows * BytesPerPixel];
        void Row(int y)
        {
            var srcRow = y * srcWidth * BytesPerPixel;
            var dstRow = y * dstWidth * BytesPerPixel;
            for (var x = 0; x < dstWidth; x++)
            {
                var c = cx[x];
                float b = 0, g = 0, r = 0;
                var si = srcRow + c.Start * BytesPerPixel;
                foreach (var w in c.Weights)
                {
                    b += float.CreateTruncating(src[si]) * w;
                    g += float.CreateTruncating(src[si + 1]) * w;
                    r += float.CreateTruncating(src[si + 2]) * w;
                    si += BytesPerPixel;
                }

                var di = dstRow + x * BytesPerPixel;
                dst[di] = b;
                dst[di + 1] = g;
                dst[di + 2] = r;
            }
        }

        RunRows(rows, (long)dstWidth * rows, Row);
        return dst;
    }

    private static float[] VerticalPass<T>(T[] src, int width, Contribution[] cy, int dstHeight)
        where T : unmanaged, INumberBase<T>
    {
        var rowLength = width * BytesPerPixel;
        var dst = new float[rowLength * dstHeight];
        void Row(int y)
        {
            var c = cy[y];
            var dstRow = y * rowLength;
            for (var k = 0; k < c.Weights.Length; k++)
            {
                var w = c.Weights[k];
                var srcRow = (c.Start + k) * rowLength;
                for (var i = 0; i < rowLength; i++)
                {
                    dst[dstRow + i] += float.CreateTruncating(src[srcRow + i]) * w;
                }
            }
        }

        RunRows(dstHeight, (long)rowLength * dstHeight, Row);
        return dst;
    }

    private static void RunRows(int rows, long work, Action<int> row)
    {
        if (work >= ParallelThreshold && rows > 1)
        {
            Parallel.For(0, rows, row);
        }
        else
        {
            for (var y = 0; y < rows; y++)
            {
                row(y);
            }
        }
    }

    private readonly record struct Contribution(int Start, float[] Weights)
    {
        public static Contribution[] Build(int source, int destination)
        {
            var result = new Contribution[destination];
            var scale = (double)source / destination;
            if (destination <= source)
            {
                // Area average: destination pixel i covers [i*scale, (i+1)*scale) of the source.
                for (var i = 0; i < destination; i++)
                {
                    var s0 = i * scale;
                    var s1 = Math.Min(source, (i + 1) * scale);
                    var start = (int)Math.Floor(s0);
                    var end = Math.Min(source, (int)Math.Ceiling(s1 - 1e-9));
                    var weights = new float[Math.Max(1, end - start)];
                    for (var k = 0; k < weights.Length; k++)
                    {
                        var overlap = Math.Min(s1, start + k + 1) - Math.Max(s0, start + k);
                        weights[k] = (float)(Math.Max(0, overlap) / scale);
                    }

                    result[i] = new Contribution(start, weights);
                }
            }
            else
            {
                // Bilinear: sample at the destination pixel's center.
                for (var i = 0; i < destination; i++)
                {
                    var x = Math.Clamp((i + 0.5) * scale - 0.5, 0, source - 1);
                    var x0 = (int)Math.Floor(x);
                    var f = (float)(x - x0);
                    result[i] = x0 + 1 < source && f > 0
                        ? new Contribution(x0, [1 - f, f])
                        : new Contribution(x0, [1f]);
                }
            }

            return result;
        }
    }
}
