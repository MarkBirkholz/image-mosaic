using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Tests;

public class MultipleFoldersTests
{
    [Fact]
    public void LoadsAllFoldersAndCountsOverlappingFilesOnce()
    {
        using var temp = new TempFolder();
        using var cacheDir = new TempFolder();
        var colors = TestImages.Palette(6).ToArray();
        for (var i = 0; i < 3; i++)
        {
            temp.SaveImage(Path.Combine("a", $"a{i}.png"), PixelBuffer.Solid(10, 10, colors[i]));
            temp.SaveImage(Path.Combine("b", "nested", $"b{i}.png"), PixelBuffer.Solid(10, 10, colors[i + 3]));
        }

        var a = Path.Combine(temp.Path, "a");
        var b = Path.Combine(temp.Path, "b");
        var nested = Path.Combine(b, "nested");
        var options = new TileLibraryOptions([a, b, nested, a], 8) { CacheDirectory = cacheDir.Path };

        var first = TileLibrary.Load(options);
        var second = TileLibrary.Load(options);

        Assert.Equal(6, first.Stats.FilesFound);
        Assert.Equal(colors.ToHashSet(), first.Tiles.Select(t => t.Mean).ToHashSet());
        Assert.Equal((6, 0), (second.Stats.FromCache, second.Stats.Decoded));
        var cache = new TileCache(cacheDir.Path);
        Assert.True(File.Exists(cache.GetCacheFile(a, 8)));
        Assert.True(File.Exists(cache.GetCacheFile(b, 8)));
    }

    [Fact]
    public void ValidationRequiresExistingFolders()
    {
        using var temp = new TempFolder();
        var missing = Path.Combine(temp.Path, "missing");

        Assert.Single(new MosaicSettings { TilesFolders = [] }.ValidateTilesFolders());
        Assert.Single(new MosaicSettings { TilesFolders = ["  "] }.ValidateTilesFolders());
        Assert.Empty(new MosaicSettings { TilesFolders = [temp.Path, " "] }.ValidateTilesFolders());
        var error = Assert.Single(new MosaicSettings { TilesFolders = [temp.Path, missing] }.ValidateTilesFolders());
        Assert.Contains(missing, error);
    }

    [Fact]
    public void BuilderUsesTilesFromEveryFolder()
    {
        using var temp = new TempFolder();
        var target = temp.SaveImage("target.png", TestImages.Gradient(64, 32));
        for (var i = 0; i < 20; i++)
        {
            var dark = (byte)(i * 6);
            var light = (byte)(135 + i * 6);
            temp.SaveImage(Path.Combine("dark", $"{i}.png"), PixelBuffer.Solid(10, 10, new Rgb(dark, dark, dark)));
            temp.SaveImage(Path.Combine("light", $"{i}.png"), PixelBuffer.Solid(10, 10, new Rgb(light, light, light)));
        }

        var settings = new MosaicSettings
        {
            TargetImagePath = target,
            TilesFolders = [Path.Combine(temp.Path, "dark"), Path.Combine(temp.Path, "light")],
            Columns = 8,
            TileSize = 8,
            MinRepeatDistance = 0,
        };

        var result = new MosaicBuilder(null).Build(settings);
        var used = result.Plan.TileIndices.Select(i => result.Library.Tiles[i].SourcePath).ToList();

        Assert.Equal(40, result.Library.Tiles.Count);
        Assert.Contains(used, p => p.Contains($"{Path.DirectorySeparatorChar}dark{Path.DirectorySeparatorChar}"));
        Assert.Contains(used, p => p.Contains($"{Path.DirectorySeparatorChar}light{Path.DirectorySeparatorChar}"));
    }
}
