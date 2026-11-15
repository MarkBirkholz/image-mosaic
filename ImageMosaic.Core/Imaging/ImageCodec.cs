using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageMosaic.Core.Imaging;

/// <summary>Size of an image as displayed, i.e. after its EXIF orientation is applied.</summary>
public readonly record struct ImageInfo(int Width, int Height, int Orientation);

/// <summary>
/// Reads and writes images with the Windows Imaging Component (via WPF's imaging classes).
/// Compared with GDI+ <c>new Bitmap(path)</c> this can decode a JPEG directly at a reduced size
/// (DCT scaling), which makes building a tile library from large photos several times faster.
/// </summary>
public static class ImageCodec
{
    public static readonly IReadOnlySet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp", ".heic", ".heif",
    };

    private const BitmapCreateOptions CreateOptions =
        BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.DelayCreation;

    public static bool IsSupported(string path) => SupportedExtensions.Contains(Path.GetExtension(path));

    public static ImageInfo ReadInfo(string path)
    {
        using var stream = OpenRead(path);
        var frame = BitmapDecoder.Create(stream, CreateOptions, BitmapCacheOption.None).Frames[0];
        return ToInfo(frame.PixelWidth, frame.PixelHeight, ReadOrientation(frame));
    }

    /// <summary>Loads an image file with its EXIF orientation applied.</summary>
    /// <param name="path">Image file.</param>
    /// <param name="minShortSide">
    /// When set, the image is decoded at a reduced size whose shorter side is at least this many pixels.
    /// Images that are already small enough are decoded at full size.
    /// </param>
    public static PixelBuffer Load(string path, int? minShortSide = null) => Decode(File.ReadAllBytes(path), minShortSide);

    public static PixelBuffer Decode(byte[] bytes, int? minShortSide = null)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var frame = BitmapDecoder.Create(stream, CreateOptions, BitmapCacheOption.None).Frames[0];
        var width = frame.PixelWidth;
        var height = frame.PixelHeight;
        var orientation = ReadOrientation(frame);

        BitmapSource source = frame;
        if (minShortSide is { } target && Math.Min(width, height) > target)
        {
            stream.Position = 0;
            var scaled = new BitmapImage();
            scaled.BeginInit();
            scaled.StreamSource = stream;
            scaled.CacheOption = BitmapCacheOption.OnLoad;
            scaled.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            // Sizes refer to the stored (not rotated) image; setting only one keeps the aspect ratio.
            if (width <= height)
            {
                scaled.DecodePixelWidth = target;
            }
            else
            {
                scaled.DecodePixelHeight = target;
            }

            scaled.EndInit();
            source = scaled;
        }

        return ToPixelBuffer(source).ApplyExifOrientation(orientation);
    }

    public static void Save(PixelBuffer image, string path, int jpegQuality = 92)
    {
        ArgumentNullException.ThrowIfNull(image);
        BitmapEncoder encoder = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = Math.Clamp(jpegQuality, 1, 100) },
            ".png" => new PngBitmapEncoder(),
            ".bmp" => new BmpBitmapEncoder(),
            ".tif" or ".tiff" => new TiffBitmapEncoder { Compression = TiffCompressOption.Lzw },
            var ext => throw new NotSupportedException($"Cannot save images as '{ext}'. Use .jpg, .png, .bmp or .tif."),
        };

        var source = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgr24, null, image.Data, image.Stride);
        encoder.Frames.Add(BitmapFrame.Create(source));

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
        encoder.Save(stream);
    }

    private static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);

    private static ImageInfo ToInfo(int width, int height, int orientation) =>
        orientation >= 5 ? new ImageInfo(height, width, orientation) : new ImageInfo(width, height, orientation);

    private static PixelBuffer ToPixelBuffer(BitmapSource source)
    {
        if (source.Format != PixelFormats.Bgr24)
        {
            source = new FormatConvertedBitmap(source, PixelFormats.Bgr24, null, 0);
        }

        var buffer = new PixelBuffer(source.PixelWidth, source.PixelHeight);
        source.CopyPixels(buffer.Data, buffer.Stride, 0);
        return buffer;
    }

    private static int ReadOrientation(BitmapFrame frame)
    {
        try
        {
            if (frame.Metadata is not BitmapMetadata metadata)
            {
                return 1;
            }

            foreach (var query in OrientationQueries)
            {
                if (metadata.ContainsQuery(query) && metadata.GetQuery(query) is ushort value)
                {
                    return value;
                }
            }
        }
        catch (Exception e) when (e is NotSupportedException or InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException)
        {
            // Formats without (readable) metadata are shown as stored.
        }

        return 1;
    }

    private static readonly string[] OrientationQueries =
    [
        "/app1/ifd/{ushort=274}", // JPEG
        "/ifd/{ushort=274}", // TIFF
    ];
}
