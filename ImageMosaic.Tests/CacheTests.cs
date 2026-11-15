using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Tests;

public class TileCacheClearingTests
{
    [Fact]
    public void ReportsUsageAndClearsOnlyCacheFiles()
    {
        using var temp = new TempFolder();
        var cache = new TileCache(temp.Path);
        cache.Save(@"C:\a", 16, new Dictionary<string, CachedTile> { ["x"] = new(1, 2, new byte[16 * 16 * 3]) });
        cache.Save(@"C:\b", 32, new Dictionary<string, CachedTile> { ["y"] = new(1, 2, null) });
        File.WriteAllText(cache.GetCacheFile(@"C:\c", 8) + ".tmp", "interrupted write");
        var unrelated = temp.File("notes.txt");
        File.WriteAllText(unrelated, "keep me");

        var (files, bytes) = cache.GetUsage();
        var (deleted, failed) = cache.Clear();

        Assert.Equal(3, files);
        Assert.True(bytes > 16 * 16 * 3);
        Assert.Equal((3, 0), (deleted, failed));
        Assert.Equal((0, 0L), cache.GetUsage());
        Assert.True(File.Exists(unrelated));
    }

    [Fact]
    public void MissingCacheFolderIsEmpty()
    {
        using var temp = new TempFolder();
        var cache = new TileCache(Path.Combine(temp.Path, "never-created"));

        Assert.Equal((0, 0L), cache.GetUsage());
        Assert.Equal((0, 0), cache.Clear());
    }

    [Fact]
    public void LibraryReloadsAfterClearing()
    {
        using var photos = new TempFolder();
        using var cacheDir = new TempFolder();
        photos.SaveImage("a.png", PixelBuffer.Solid(10, 10, new Rgb(1, 2, 3)));
        var options = new TileLibraryOptions(photos.Path, 8) { CacheDirectory = cacheDir.Path };

        TileLibrary.Load(options);
        new TileCache(cacheDir.Path).Clear();
        var reloaded = TileLibrary.Load(options);

        Assert.Equal((0, 1), (reloaded.Stats.FromCache, reloaded.Stats.Decoded));
    }
}
