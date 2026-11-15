using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Mosaic;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Tests;

public class TargetGridTests
{
    [Theory]
    [InlineData(80, 1600, 900, 80, 45)]
    [InlineData(80, 900, 1600, 45, 80)]
    [InlineData(10, 100, 100, 10, 10)]
    [InlineData(10, 1000, 10, 10, 1)]
    [InlineData(10, 10, 1000, 1, 10)]
    public void LongSideGetsTheTilesAndTheOtherKeepsTheAspectRatio(int longSide, int width, int height, int columns, int rows)
    {
        Assert.Equal((columns, rows), TargetGrid.SizeFor(longSide, width, height));
    }

    [Fact]
    public void CellsHoldSubCellColors()
    {
        // 2×1 cells; each cell is white on top and black at the bottom.
        var image = new PixelBuffer(20, 10);
        for (var y = 0; y < 10; y++)
        {
            for (var x = 0; x < 20; x++)
            {
                image.SetPixel(x, y, y < 5 ? new Rgb(255, 255, 255) : new Rgb(0, 0, 0));
            }
        }

        var grid = TargetGrid.FromImage(image, 2, 1, 2);
        var cell = grid.CellFeatures(1);

        Assert.Equal(2, grid.CellCount);
        Assert.Equal(100, cell[0].L, 0);
        Assert.Equal(0, cell[3].L, 0);
        Assert.Equal(new Rgb(128, 128, 128), grid.Means[0]);
    }
}

public class TileMatcherTests
{
    private static readonly MatchOptions ClosestOnly = new() { MinRepeatDistance = 0, UsagePenalty = 0 };

    [Fact]
    public void PicksTheTileWithTheSameColor()
    {
        var palette = TestImages.Palette(40).ToArray();
        var library = TileLibrary.FromPixels(palette.Select(c => PixelBuffer.Solid(4, 4, c)));
        // Target: 8×5 cells of 6×6 px, each exactly one palette color.
        var target = new PixelBuffer(48, 30);
        for (var i = 0; i < 40; i++)
        {
            for (var p = 0; p < 36; p++)
            {
                target.SetPixel(i % 8 * 6 + p % 6, i / 8 * 6 + p / 6, palette[i]);
            }
        }

        var plan = TileMatcher.Match(TargetGrid.FromImage(target, 8, 5, 3), library, ClosestOnly);

        for (var i = 0; i < 40; i++)
        {
            Assert.Equal(palette[i], library.Tiles[plan.TileIndices[i]].Mean);
        }

        Assert.True(plan.MeanError < 0.5);
    }

    [Fact]
    public void ColorGridDistinguishesLayoutWithSameAverage()
    {
        var white = new Rgb(255, 255, 255);
        var black = new Rgb(0, 0, 0);
        var lightTop = TestImages.TwoTone(8, white, black);
        var darkTop = TestImages.TwoTone(8, black, white);
        var library = TileLibrary.FromPixels([darkTop, lightTop], featureGrid: 2);

        var plan = TileMatcher.Match(TargetGrid.FromImage(lightTop, 1, 1, 2), library, ClosestOnly);

        Assert.Equal(1, plan.TileIndices[0]);
    }

    [Fact]
    public void SameTileIsNeverCloserThanTheRepeatDistance()
    {
        var library = TileLibrary.FromPixels(Enumerable.Range(0, 60).Select(i => PixelBuffer.Solid(4, 4, new Rgb((byte)(100 + i), 100, 100))), featureGrid: 1);
        var target = PixelBuffer.Solid(30, 20, new Rgb(130, 100, 100));
        var options = new MatchOptions { MinRepeatDistance = 3, UsagePenalty = 0 };

        var plan = TileMatcher.Match(TargetGrid.FromImage(target, 30, 20, 1), library, options);

        Assert.Equal(3, plan.RepeatDistance);
        for (var a = 0; a < plan.TileIndices.Length; a++)
        {
            for (var b = a + 1; b < plan.TileIndices.Length; b++)
            {
                if (plan.TileIndices[a] != plan.TileIndices[b])
                {
                    continue;
                }

                var distance = Math.Max(Math.Abs(a % 30 - b % 30), Math.Abs(a / 30 - b / 30));
                Assert.True(distance > 3, $"Tile {plan.TileIndices[a]} repeats {distance} cells apart.");
            }
        }
    }

    [Fact]
    public void RepeatDistanceShrinksForSmallLibraries()
    {
        var library = TileLibrary.FromPixels(TestImages.Palette(5).Select(c => PixelBuffer.Solid(4, 4, c)), featureGrid: 1);
        var target = PixelBuffer.Solid(10, 10, new Rgb(1, 2, 3));

        var plan = TileMatcher.Match(TargetGrid.FromImage(target, 10, 10, 1), library, new MatchOptions { MinRepeatDistance = 4 });

        Assert.Equal(0, plan.RepeatDistance);
        Assert.All(plan.TileIndices, i => Assert.InRange(i, 0, 4));
    }

    [Fact]
    public void UsagePenaltySpreadsTilesAround()
    {
        var library = TileLibrary.FromPixels(Enumerable.Range(0, 50).Select(i => PixelBuffer.Solid(4, 4, new Rgb((byte)(100 + i), 120, 140))), featureGrid: 1);
        var grid = TargetGrid.FromImage(PixelBuffer.Solid(20, 20, new Rgb(100, 120, 140)), 20, 20, 1);

        var greedy = TileMatcher.Match(grid, library, ClosestOnly);
        var varied = TileMatcher.Match(grid, library, ClosestOnly with { UsagePenalty = 10 });

        Assert.Equal(1, greedy.UniqueTiles);
        Assert.True(varied.UniqueTiles > 20, $"Only {varied.UniqueTiles} tiles used.");
    }

    [Fact]
    public void SameSeedGivesSameMosaic()
    {
        var library = TileLibrary.FromPixels(TestImages.Palette(80).Select(c => PixelBuffer.Solid(4, 4, c)));
        var grid = TargetGrid.FromImage(TestImages.Gradient(64, 48), 16, 12, 3);

        var a = TileMatcher.Match(grid, library, new MatchOptions { Seed = 7 });
        var b = TileMatcher.Match(grid, library, new MatchOptions { Seed = 7 });

        Assert.Equal(a.TileIndices, b.TileIndices);
    }

    [Fact]
    public void RejectsMismatchedFeatureGrids()
    {
        var library = TileLibrary.FromPixels([PixelBuffer.Solid(4, 4, new Rgb(1, 1, 1))], featureGrid: 3);
        var grid = TargetGrid.FromImage(PixelBuffer.Solid(4, 4, new Rgb(1, 1, 1)), 1, 1, 2);

        Assert.Throws<ArgumentException>(() => TileMatcher.Match(grid, library, new MatchOptions()));
    }
}

public class MosaicRendererTests
{
    private static (TileLibrary Library, TargetGrid Grid, MosaicPlan Plan, PixelBuffer Target) Setup()
    {
        var tiles = new[]
        {
            TestImages.TwoTone(6, new Rgb(200, 0, 0), new Rgb(0, 0, 200)),
            PixelBuffer.Solid(6, 6, new Rgb(30, 160, 30)),
        };
        var library = TileLibrary.FromPixels(tiles);
        var target = TestImages.Gradient(18, 12); // 3×2 cells of 6 px
        var grid = TargetGrid.FromImage(target, 3, 2, 3);
        var plan = new MosaicPlan(3, 2, [0, 1, 0, 1, 0, 1], new float[6], 0);
        return (library, grid, plan, target);
    }

    [Fact]
    public void WithoutAdjustmentsTilesAreCopiedExactly()
    {
        var (library, grid, plan, target) = Setup();

        var image = MosaicRenderer.Render(plan, library, grid, target, new RenderOptions { ColorCorrection = 0, Overlay = 0 });

        Assert.Equal((18, 12), (image.Width, image.Height));
        Assert.Equal(library.Tiles[0].Pixels.Data, image.Crop(0, 0, 6, 6).Data);
        Assert.Equal(library.Tiles[1].Pixels.Data, image.Crop(6, 0, 6, 6).Data);
        Assert.Equal(library.Tiles[1].Pixels.Data, image.Crop(0, 6, 6, 6).Data);
    }

    [Fact]
    public void FullOverlayShowsTheTarget()
    {
        var (library, grid, plan, target) = Setup();

        var image = MosaicRenderer.Render(plan, library, grid, target, new RenderOptions { ColorCorrection = 0, Overlay = 1 });

        Assert.Equal(target.Data, image.Data);
    }

    [Fact]
    public void FullColorCorrectionMatchesCellAverages()
    {
        var (library, grid, plan, target) = Setup();

        var image = MosaicRenderer.Render(plan, library, grid, target, new RenderOptions { ColorCorrection = 1, Overlay = 0 });

        // The solid green tile in cell (1, 0) becomes exactly that cell's average gray.
        Assert.Equal(grid.Means[1], image.Crop(6, 0, 6, 6).Average());
    }
}

public class MosaicSettingsTests
{
    [Fact]
    public void ReportsMissingInputs()
    {
        var errors = new MosaicSettings().Validate();

        Assert.Contains(errors, e => e.Contains("target picture", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, e => e.Contains("folder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReportsOutOfRangeNumbers()
    {
        using var temp = new TempFolder();
        var settings = new MosaicSettings
        {
            TargetImagePath = temp.SaveImage("t.png", new PixelBuffer(4, 4)),
            TilesFolders = [temp.Path],
            LongSideTiles = 1,
            TileSize = 1000,
            Overlay = 2,
            EdgeBlend = 0.3,
        };

        var errors = settings.Validate();

        Assert.Equal(4, errors.Count);
        Assert.Contains(errors, e => e.Contains("25 %"));
    }

    [Fact]
    public void ValidSettingsHaveNoErrors()
    {
        using var temp = new TempFolder();
        var settings = new MosaicSettings { TargetImagePath = temp.SaveImage("t.jpg", new PixelBuffer(4, 4)), TilesFolders = [temp.Path] };

        Assert.Empty(settings.Validate());
    }

    [Fact]
    public void OutputSizeFollowsTarget()
    {
        var settings = new MosaicSettings { LongSideTiles = 50, TileSize = 20 };

        Assert.Equal((1000, 560, 50, 28), settings.OutputSize(4000, 2250));
        Assert.Equal((560, 1000, 28, 50), settings.OutputSize(2250, 4000));
    }
}

public class MosaicBuilderTests
{
    [Fact]
    public void BuildsAMosaicThatFollowsTheTarget()
    {
        using var temp = new TempFolder();
        var target = temp.SaveImage("target.png", TestImages.Gradient(320, 160));
        for (var i = 0; i < 64; i++)
        {
            var v = (byte)(i * 4);
            temp.SaveImage(Path.Combine("tiles", $"gray{i:D2}.png"), PixelBuffer.Solid(30, 20, new Rgb(v, v, v)));
        }

        var settings = new MosaicSettings
        {
            TargetImagePath = target,
            TilesFolders = [Path.Combine(temp.Path, "tiles")],
            LongSideTiles = 16,
            TileSize = 8,
            MinRepeatDistance = 1,
            ColorCorrection = 0,
            Overlay = 0,
        };
        var log = new List<string>();

        var result = new MosaicBuilder(cacheDirectory: null, log.Add).Build(settings);

        Assert.Equal((128, 64), (result.Image.Width, result.Image.Height));
        Assert.Equal(64, result.Library.Tiles.Count);
        Assert.True(result.Plan.MeanError < 6, $"Mean error {result.Plan.MeanError}");
        var leftColumn = result.Image.Crop(0, 0, 8, 64).Average().R;
        var rightColumn = result.Image.Crop(120, 0, 8, 64).Average().R;
        Assert.True(leftColumn < 30 && rightColumn > 220, $"left {leftColumn}, right {rightColumn}");
        Assert.Contains(log, l => l.StartsWith("Done", StringComparison.Ordinal));
    }

    [Fact]
    public void RerenderKeepsThePlan()
    {
        using var temp = new TempFolder();
        var target = temp.SaveImage("target.png", TestImages.Gradient(64, 32));
        foreach (var (color, i) in TestImages.Palette(30).Select((c, i) => (c, i)))
        {
            temp.SaveImage(Path.Combine("tiles", $"{i}.png"), PixelBuffer.Solid(10, 10, color));
        }

        var settings = new MosaicSettings { TargetImagePath = target, TilesFolders = [Path.Combine(temp.Path, "tiles")], LongSideTiles = 8, TileSize = 8 };
        var first = new MosaicBuilder(null).Build(settings);

        var second = MosaicBuilder.Rerender(first, colorCorrection: 1, overlay: 0.5);

        Assert.Same(first.Plan, second.Plan);
        Assert.Equal(1, second.Settings.ColorCorrection);
        Assert.NotEqual(first.Image.Data, second.Image.Data);
    }

    [Fact]
    public void RefusesInvalidSettings()
    {
        Assert.Throws<ArgumentException>(() => new MosaicBuilder(null).Build(new MosaicSettings()));
    }

    [Fact]
    public void RefusesHugeOutput()
    {
        using var temp = new TempFolder();
        var settings = new MosaicSettings
        {
            TargetImagePath = temp.SaveImage("tall.png", new PixelBuffer(10, 100)),
            TilesFolders = [temp.Path],
            LongSideTiles = 500,
            TileSize = 256,
        };

        var error = Assert.Throws<ArgumentException>(() => new MosaicBuilder(null).Build(settings));
        Assert.Contains("megapixels", error.Message);
    }

    [Fact]
    public void ReportsAnEmptyTilesFolder()
    {
        using var temp = new TempFolder();
        var target = temp.SaveImage("target.png", new PixelBuffer(8, 8));
        Directory.CreateDirectory(temp.File(Path.Combine("empty", "x")));
        var settings = new MosaicSettings { TargetImagePath = target, TilesFolders = [Path.Combine(temp.Path, "empty")], LongSideTiles = 4, TileSize = 8 };

        Assert.Throws<InvalidOperationException>(() => new MosaicBuilder(null).Build(settings));
    }
}
