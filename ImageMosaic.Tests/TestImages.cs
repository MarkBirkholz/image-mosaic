using ImageMosaic.Core.Imaging;

namespace ImageMosaic.Tests;

/// <summary>Synthetic images and a temporary folder that is deleted after the test.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ImageMosaicTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string relative)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        return full;
    }

    public string SaveImage(string relative, PixelBuffer image)
    {
        var path = File(relative);
        ImageCodec.Save(image, path);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

internal static class TestImages
{
    /// <summary>Horizontal gradient from black (left) to white (right) — each column has a distinct value.</summary>
    public static PixelBuffer Gradient(int width, int height)
    {
        var image = new PixelBuffer(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var v = (byte)(width == 1 ? 0 : x * 255 / (width - 1));
                image.SetPixel(x, y, new Rgb(v, v, v));
            }
        }

        return image;
    }

    /// <summary>Each pixel gets a unique color from its coordinates, so geometric transforms can be checked exactly.</summary>
    public static PixelBuffer Coordinates(int width, int height)
    {
        var image = new PixelBuffer(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetPixel(x, y, new Rgb((byte)x, (byte)y, 7));
            }
        }

        return image;
    }

    /// <summary>A tile whose top half and bottom half have different colors.</summary>
    public static PixelBuffer TwoTone(int size, Rgb top, Rgb bottom)
    {
        var image = new PixelBuffer(size, size);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                image.SetPixel(x, y, y < size / 2 ? top : bottom);
            }
        }

        return image;
    }

    /// <summary>Distinct solid colors spread over the RGB cube.</summary>
    public static IEnumerable<Rgb> Palette(int count)
    {
        var random = new Random(count);
        for (var i = 0; i < count; i++)
        {
            yield return new Rgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
        }
    }
}
