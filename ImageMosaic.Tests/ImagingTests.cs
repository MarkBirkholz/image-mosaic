using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMosaic.Core.Imaging;

namespace ImageMosaic.Tests;

public class ColorMathTests
{
    [Fact]
    public void BlackAndWhiteAreEndsOfLightness()
    {
        var black = ColorMath.ToLab(new Rgb(0, 0, 0));
        var white = ColorMath.ToLab(new Rgb(255, 255, 255));

        Assert.Equal(0, black.L, 2);
        Assert.Equal(100, white.L, 1);
        Assert.Equal(0, white.A, 1);
        Assert.Equal(0, white.B, 1);
    }

    [Fact]
    public void PureRedMatchesReferenceLab()
    {
        var red = ColorMath.ToLab(new Rgb(255, 0, 0));

        Assert.Equal(53.24, red.L, 1);
        Assert.Equal(80.09, red.A, 1);
        Assert.Equal(67.20, red.B, 1);
    }

    [Fact]
    public void DistanceIsSymmetricAndZeroForSameColor()
    {
        var a = ColorMath.ToLab(new Rgb(10, 200, 30));
        var b = ColorMath.ToLab(new Rgb(200, 20, 90));

        Assert.Equal(0, ColorMath.Distance(a, a));
        Assert.Equal(ColorMath.Distance(a, b), ColorMath.Distance(b, a));
        Assert.True(ColorMath.Distance(a, b) > 50);
    }
}

public class PixelBufferTests
{
    [Fact]
    public void ShrinkingAveragesExactly()
    {
        var image = new PixelBuffer(2, 2);
        image.SetPixel(0, 0, new Rgb(0, 0, 0));
        image.SetPixel(1, 0, new Rgb(100, 40, 8));
        image.SetPixel(0, 1, new Rgb(200, 80, 16));
        image.SetPixel(1, 1, new Rgb(100, 40, 0));

        var pixel = image.Resize(1, 1).GetPixel(0, 0);

        Assert.Equal(new Rgb(100, 40, 6), pixel);
    }

    [Fact]
    public void ShrinkingUsesWholeBlocks()
    {
        var image = new PixelBuffer(4, 1);
        image.SetPixel(0, 0, new Rgb(0, 0, 0));
        image.SetPixel(1, 0, new Rgb(100, 100, 100));
        image.SetPixel(2, 0, new Rgb(200, 200, 200));
        image.SetPixel(3, 0, new Rgb(250, 250, 250));

        var small = image.Resize(2, 1);

        Assert.Equal(50, small.GetPixel(0, 0).R);
        Assert.Equal(225, small.GetPixel(1, 0).R);
    }

    [Theory]
    [InlineData(7, 5)]
    [InlineData(3, 2)]
    [InlineData(40, 31)]
    [InlineData(1, 1)]
    public void SolidColorSurvivesAnyResize(int width, int height)
    {
        var color = new Rgb(12, 130, 250);
        var resized = PixelBuffer.Solid(13, 9, color).Resize(width, height);

        Assert.Equal(width, resized.Width);
        Assert.Equal(height, resized.Height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                Assert.Equal(color, resized.GetPixel(x, y));
            }
        }
    }

    [Fact]
    public void ShrinkingKeepsTheMean()
    {
        var gradient = TestImages.Gradient(256, 10);

        var small = gradient.Resize(17, 3);

        Assert.InRange(small.Average().R, gradient.Average().R - 1, gradient.Average().R + 1);
    }

    [Fact]
    public void EnlargingInterpolatesBetweenNeighbours()
    {
        var image = new PixelBuffer(2, 1);
        image.SetPixel(0, 0, new Rgb(0, 0, 0));
        image.SetPixel(1, 0, new Rgb(200, 200, 200));

        var big = image.Resize(8, 1);
        var values = Enumerable.Range(0, 8).Select(x => big.GetPixel(x, 0).R).ToArray();

        Assert.Equal(0, values[0]);
        Assert.Equal(200, values[7]);
        Assert.Equal(values.OrderBy(v => v), values);
    }

    [Fact]
    public void CropCenterTakesTheMiddle()
    {
        var image = TestImages.Coordinates(30, 10);

        var square = image.CropCenter(1.0);

        Assert.Equal(10, square.Width);
        Assert.Equal(10, square.Height);
        Assert.Equal(10, square.GetPixel(0, 0).R);
        Assert.Equal(19, square.GetPixel(9, 9).R);
    }

    [Fact]
    public void CropOutsideTheImageThrows()
    {
        var image = new PixelBuffer(10, 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => image.Crop(5, 5, 6, 2));
    }

    [Theory]
    // orientation, expected source pixel (x, y) at the result's top-left and top-right corners
    [InlineData(1, 0, 0, 3, 0)]
    [InlineData(2, 3, 0, 0, 0)]
    [InlineData(3, 3, 2, 0, 2)]
    [InlineData(4, 0, 2, 3, 2)]
    [InlineData(5, 0, 0, 0, 2)]
    [InlineData(6, 0, 2, 0, 0)]
    [InlineData(7, 3, 2, 3, 0)]
    [InlineData(8, 3, 0, 3, 2)]
    public void ExifOrientationMapsCorners(int orientation, int tlx, int tly, int trx, int try_)
    {
        var image = TestImages.Coordinates(4, 3);

        var oriented = image.ApplyExifOrientation(orientation);

        var rotated = orientation >= 5;
        Assert.Equal(rotated ? 3 : 4, oriented.Width);
        Assert.Equal(rotated ? 4 : 3, oriented.Height);
        Assert.Equal(image.GetPixel(tlx, tly), oriented.GetPixel(0, 0));
        Assert.Equal(image.GetPixel(trx, try_), oriented.GetPixel(oriented.Width - 1, 0));
    }

    [Fact]
    public void ConstructorRejectsWrongBufferSize()
    {
        Assert.Throws<ArgumentException>(() => new PixelBuffer(2, 2, new byte[11]));
    }
}

public class ImageCodecTests
{
    [Fact]
    public void PngRoundTripIsLossless()
    {
        using var temp = new TempFolder();
        var image = TestImages.Coordinates(37, 23);

        var path = temp.SaveImage("a.png", image);
        var loaded = ImageCodec.Load(path);

        Assert.Equal(image.Data, loaded.Data);
    }

    [Fact]
    public void JpegRoundTripIsClose()
    {
        using var temp = new TempFolder();
        var image = TestImages.Gradient(64, 32);

        var loaded = ImageCodec.Load(temp.SaveImage("a.jpg", image));

        Assert.Equal(64, loaded.Width);
        Assert.InRange(loaded.Average().R, image.Average().R - 2, image.Average().R + 2);
    }

    [Fact]
    public void LoadsReducedSizeWhenAsked()
    {
        using var temp = new TempFolder();
        var path = temp.SaveImage("wide.jpg", TestImages.Gradient(400, 200));

        var small = ImageCodec.Load(path, minShortSide: 50);

        Assert.Equal(50, small.Height);
        Assert.Equal(100, small.Width);
    }

    [Fact]
    public void DoesNotEnlargeSmallImages()
    {
        using var temp = new TempFolder();
        var path = temp.SaveImage("small.png", TestImages.Gradient(40, 20));

        var loaded = ImageCodec.Load(path, minShortSide: 100);

        Assert.Equal(40, loaded.Width);
    }

    [Fact]
    public void AppliesExifOrientation()
    {
        using var temp = new TempFolder();
        var path = temp.File("rotated.jpg");
        var image = TestImages.TwoTone(8, new Rgb(250, 250, 250), new Rgb(0, 0, 0)).Resize(16, 8);
        WriteJpegWithOrientation(image, path, orientation: 6);

        var info = ImageCodec.ReadInfo(path);
        var loaded = ImageCodec.Load(path);

        Assert.Equal((8, 16, 6), (info.Width, info.Height, info.Orientation));
        Assert.Equal(8, loaded.Width);
        Assert.Equal(16, loaded.Height);
        // Stored top (light) half ends up on the right after a clockwise turn.
        Assert.True(loaded.GetPixel(7, 8).R > 200);
        Assert.True(loaded.GetPixel(0, 8).R < 50);
    }

    [Theory]
    [InlineData("photo.JPG", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("photo.Png", true)]
    [InlineData("photo.webp", true)]
    [InlineData("notes.txt", false)]
    [InlineData("noextension", false)]
    public void RecognizesImageExtensionsInAnyCase(string name, bool expected)
    {
        Assert.Equal(expected, ImageCodec.IsSupported(name));
    }

    [Fact]
    public void SavingToUnknownFormatThrows()
    {
        using var temp = new TempFolder();

        Assert.Throws<NotSupportedException>(() => ImageCodec.Save(new PixelBuffer(2, 2), temp.File("a.xyz")));
    }

    private static void WriteJpegWithOrientation(PixelBuffer image, string path, ushort orientation)
    {
        var source = BitmapSource.Create(image.Width, image.Height, 96, 96, PixelFormats.Bgr24, null, image.Data, image.Stride);
        var metadata = new BitmapMetadata("jpg");
        metadata.SetQuery("/app1/ifd/{ushort=274}", orientation);
        var encoder = new JpegBitmapEncoder { QualityLevel = 100 };
        encoder.Frames.Add(BitmapFrame.Create(source, null, metadata, null));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
