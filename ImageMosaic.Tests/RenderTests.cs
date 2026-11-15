using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Mosaic;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Tests;

public class TileEdgeTests
{
    private static readonly Rgb Black = new(0, 0, 0);
    private static readonly Rgb White = new(250, 250, 250);

    // Two tiles side by side: black | white, 10 px each.
    private static (MosaicPlan, TileLibrary, TargetGrid, PixelBuffer) TwoTiles()
    {
        var library = TileLibrary.FromPixels([PixelBuffer.Solid(10, 10, Black), PixelBuffer.Solid(10, 10, White)]);
        var target = new PixelBuffer(20, 10);
        var grid = TargetGrid.FromImage(target, 2, 1, 3);
        return (new MosaicPlan(2, 1, [0, 1], new float[2], 0), library, grid, target);
    }

    private static PixelBuffer Render(double blend)
    {
        var (plan, library, grid, target) = TwoTiles();
        return MosaicRenderer.Render(plan, library, grid, target, new RenderOptions { ColorCorrection = 0, Overlay = 0, EdgeBlend = blend });
    }

    [Fact]
    public void ZeroKeepsSharpEdges()
    {
        var image = Render(0);

        Assert.Equal(Black, image.GetPixel(9, 5));
        Assert.Equal(White, image.GetPixel(10, 5));
    }

    [Fact]
    public void BlendingFadesAcrossTheSeamOnly()
    {
        var image = Render(1); // band of a whole tile: fades from center to center
        var row = Enumerable.Range(0, 20).Select(x => (int)image.GetPixel(x, 5).R).ToList();

        Assert.Equal(row.OrderBy(v => v), row); // monotonic from black to white
        Assert.InRange(row[9], 100, 135); // about half way at the seam
        Assert.InRange(row[10], 115, 150);
        Assert.Equal(0, row[0]); // the outer edges of the mosaic keep their own tile
        Assert.Equal(250, row[19]);
    }

    [Fact]
    public void HalfBlendLeavesTileCentersUntouched()
    {
        var image = Render(0.5); // band of half a tile: 2.5 px on each side of the seam

        Assert.Equal(Black, image.GetPixel(5, 5));
        Assert.Equal(White, image.GetPixel(14, 5));
        Assert.NotEqual(Black, image.GetPixel(9, 5));
    }

    [Fact]
    public void UniformTilesStayUniform()
    {
        var library = TileLibrary.FromPixels([PixelBuffer.Solid(8, 8, new Rgb(90, 120, 30))]);
        var target = new PixelBuffer(24, 24);
        var grid = TargetGrid.FromImage(target, 3, 3, 3);
        var plan = new MosaicPlan(3, 3, new int[9], new float[9], 0);

        var image = MosaicRenderer.Render(plan, library, grid, target, new RenderOptions { ColorCorrection = 0, Overlay = 0, EdgeBlend = 0.7 });

        Assert.Equal(PixelBuffer.Solid(24, 24, new Rgb(90, 120, 30)).Data, image.Data);
    }
}

public class FragmentTests
{
    [Fact]
    public void FragmentAtTheSameTileSizeMatchesTheMosaic()
    {
        using var temp = new TempFolder();
        var result = Build(temp);
        var fragment = new CellRect(2, 1, 4, 2);

        var image = MosaicBuilder.RenderFragment(result, fragment, result.Library.TileSize);

        var size = result.Library.TileSize;
        var expected = result.Image.Crop(2 * size, 1 * size, 4 * size, 2 * size);
        Assert.Equal((expected.Width, expected.Height), (image.Width, image.Height));
        var difference = expected.Data.Zip(image.Data, (a, b) => Math.Abs(a - b)).Max();
        Assert.True(difference <= 2, $"max difference {difference}");
    }

    [Fact]
    public void BiggerTilesAreReadAgainFromThePhotos()
    {
        using var temp = new TempFolder();
        var result = Build(temp);

        var image = MosaicBuilder.RenderFragment(result, new CellRect(0, 0, 3, 2), 64);

        Assert.Equal((192, 128), (image.Width, image.Height));
        // Each tile photo is a two-tone square, so a sharp re-read keeps a crisp edge in the middle of the tile.
        var tile = image.Crop(0, 0, 64, 64);
        Assert.NotEqual(tile.GetPixel(32, 10), tile.GetPixel(32, 54));
    }

    [Fact]
    public void CachedPhotosGiveTheSameFragmentAndAreReadOnce()
    {
        using var temp = new TempFolder();
        var result = Build(temp);
        var frame = new CellRect(0, 0, 3, 2);
        var cache = new FragmentTileCache();

        var first = MosaicBuilder.RenderFragment(result, frame, 64, cache: cache);
        var readOnce = cache.Loaded;
        var again = MosaicBuilder.RenderFragment(result, frame, 64, cache: cache);
        var readAgain = cache.Loaded - readOnce;
        var moved = MosaicBuilder.RenderFragment(result, frame with { Column = 1 }, 64, cache: cache);

        Assert.Equal(MosaicBuilder.RenderFragment(result, frame, 64).Data, first.Data);
        Assert.Equal(first.Data, again.Data);
        // The fragment is rendered with a margin of one tile: 4 × 3 cells at the corner of the mosaic.
        Assert.InRange(readOnce, 1, 12);
        Assert.Equal(0, readAgain);
        // Moving by one column adds at most one new column of 3 cells.
        Assert.InRange(cache.Loaded - readOnce, 0, 3);
    }

    [Fact]
    public void SmallerTilesAreMadeFromTheCachedBiggerOnes()
    {
        using var temp = new TempFolder();
        var result = Build(temp);
        var cache = new FragmentTileCache();
        MosaicBuilder.RenderFragment(result, new CellRect(0, 0, 3, 2), 64, cache: cache);
        var read = cache.Loaded;

        var smaller = MosaicBuilder.RenderFragment(result, new CellRect(0, 0, 3, 2), 40, cache: cache);

        Assert.Equal(read, cache.Loaded);
        Assert.Equal((120, 80), (smaller.Width, smaller.Height));
        var tile = smaller.Crop(0, 0, 40, 40);
        Assert.NotEqual(tile.GetPixel(20, 6), tile.GetPixel(20, 34));
    }

    [Fact]
    public void CacheStaysWithinItsSize()
    {
        using var temp = new TempFolder();
        var result = Build(temp);
        // Room for about three 64 px tiles.
        var cache = new FragmentTileCache(maxBytes: 3 * 64 * 64 * 3 + 100);

        var image = MosaicBuilder.RenderFragment(result, new CellRect(0, 0, 3, 2), 64, cache: cache);

        Assert.Equal(MosaicBuilder.RenderFragment(result, new CellRect(0, 0, 3, 2), 64).Data, image.Data);
    }

    [Fact]
    public void FragmentMustLieInsideTheMosaic()
    {
        using var temp = new TempFolder();
        var result = Build(temp);

        Assert.Throws<ArgumentOutOfRangeException>(() => MosaicBuilder.RenderFragment(result, new CellRect(7, 0, 4, 2), 16));
    }

    private static MosaicResult Build(TempFolder temp)
    {
        var target = temp.SaveImage("target.png", TestImages.Gradient(160, 80));
        foreach (var (color, i) in TestImages.Palette(30).Select((c, i) => (c, i)))
        {
            temp.SaveImage(Path.Combine("tiles", $"{i}.png"), TestImages.TwoTone(64, color, new Rgb((byte)(255 - color.R), color.G, color.B)));
        }

        var settings = new MosaicSettings
        {
            TargetImagePath = target,
            TilesFolders = [Path.Combine(temp.Path, "tiles")],
            LongSideTiles = 8,
            TileSize = 16,
            ColorCorrection = 0.3,
            Overlay = 0.2,
            EdgeBlend = 0.2,
        };
        return new MosaicBuilder(null).Build(settings);
    }
}
