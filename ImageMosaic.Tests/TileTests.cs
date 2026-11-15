using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Tests;

public class ImageFileScannerTests
{
    [Fact]
    public void FindsImagesRecursivelyInAnyCaseAndSkipsOthers()
    {
        using var temp = new TempFolder();
        var tiny = new PixelBuffer(2, 2);
        temp.SaveImage("b.png", tiny);
        temp.SaveImage("A.JPG", tiny);
        temp.SaveImage(Path.Combine("sub", "deeper", "c.jpeg"), tiny);
        File.WriteAllText(temp.File("notes.txt"), "not an image");
        var hidden = temp.SaveImage("hidden.png", tiny);
        File.SetAttributes(hidden, FileAttributes.Hidden);

        var files = ImageFileScanner.Scan(temp.Path).Select(Path.GetFileName).ToList();

        Assert.Equal(["A.JPG", "b.png", "c.jpeg"], files);
    }
}

public class TileCacheTests
{
    [Fact]
    public void RoundTripsEntries()
    {
        using var temp = new TempFolder();
        var cache = new TileCache(temp.Path);
        var pixels = Enumerable.Range(0, 4 * 4 * 3).Select(i => (byte)i).ToArray();
        var entries = new Dictionary<string, CachedTile>
        {
            [@"C:\photos\a.jpg"] = new(123, 456, pixels),
            [@"C:\photos\broken.jpg"] = new(7, 8, null),
        };

        cache.Save(@"C:\photos", 4, entries);
        var loaded = cache.Load(@"C:\photos", 4);

        Assert.Equal(2, loaded.Count);
        Assert.Equal(pixels, loaded[@"c:\PHOTOS\A.jpg"].Pixels);
        Assert.Equal(123, loaded[@"C:\photos\a.jpg"].Length);
        Assert.Null(loaded[@"C:\photos\broken.jpg"].Pixels);
    }

    [Fact]
    public void KeepsFoldersAndTileSizesApart()
    {
        using var temp = new TempFolder();
        var cache = new TileCache(temp.Path);

        Assert.NotEqual(cache.GetCacheFile(@"C:\a", 32), cache.GetCacheFile(@"C:\b", 32));
        Assert.NotEqual(cache.GetCacheFile(@"C:\a", 32), cache.GetCacheFile(@"C:\a", 16));
        Assert.Equal(cache.GetCacheFile(@"C:\a", 32), cache.GetCacheFile(@"c:\A\", 32));
    }

    [Fact]
    public void DamagedCacheIsIgnored()
    {
        using var temp = new TempFolder();
        var cache = new TileCache(temp.Path);
        cache.Save("x", 4, new Dictionary<string, CachedTile> { ["a"] = new(1, 2, new byte[48]) });
        var file = cache.GetCacheFile("x", 4);
        var bytes = File.ReadAllBytes(file);
        File.WriteAllBytes(file, bytes[..^10]);

        Assert.Empty(cache.Load("x", 4));
    }
}

public class TileLibraryTests
{
    private static readonly Rgb[] Colors =
    [
        new(220, 30, 30), new(30, 200, 40), new(40, 60, 220), new(240, 240, 240), new(20, 20, 20),
    ];

    [Fact]
    public void LoadsDistinctReadableImages()
    {
        using var temp = new TempFolder();
        for (var i = 0; i < Colors.Length; i++)
        {
            temp.SaveImage($"tile{i}.png", PixelBuffer.Solid(40, 30, Colors[i]));
        }

        temp.SaveImage(Path.Combine("copies", "same.png"), PixelBuffer.Solid(40, 30, Colors[0]));
        File.WriteAllText(temp.File("broken.jpg"), "this is not a jpeg");
        var log = new List<string>();

        var library = TileLibrary.Load(new TileLibraryOptions(temp.Path, 8) { CacheDirectory = null }, log: log.Add);

        Assert.Equal(5, library.Tiles.Count);
        Assert.Equal(7, library.Stats.FilesFound);
        Assert.Equal(1, library.Stats.Duplicates);
        Assert.Equal(1, library.Stats.Failed);
        Assert.Contains(log, line => line.Contains("broken.jpg"));
        Assert.All(library.Tiles, t => Assert.Equal((8, 8), (t.Pixels.Width, t.Pixels.Height)));
        Assert.Equal(Colors.ToHashSet(), library.Tiles.Select(t => t.Mean).ToHashSet());
    }

    [Fact]
    public void CropsTheCenterInsteadOfStretching()
    {
        using var temp = new TempFolder();
        // Red | green | red, each a third of the width: the centered square is all green.
        var image = new PixelBuffer(300, 100);
        for (var y = 0; y < 100; y++)
        {
            for (var x = 0; x < 300; x++)
            {
                image.SetPixel(x, y, x is >= 100 and < 200 ? new Rgb(0, 255, 0) : new Rgb(255, 0, 0));
            }
        }

        var tile = TileLibrary.PrepareTile(temp.SaveImage("wide.png", image), 16);

        Assert.Equal(new Rgb(0, 255, 0), tile.Average());
    }

    [Fact]
    public void SecondLoadComesFromCacheAndChangedFilesAreReloaded()
    {
        using var photos = new TempFolder();
        using var cacheDir = new TempFolder();
        var paths = Colors.Select((c, i) => photos.SaveImage($"p{i}.png", PixelBuffer.Solid(20, 20, c))).ToArray();
        var options = new TileLibraryOptions(photos.Path, 8) { CacheDirectory = cacheDir.Path };

        var first = TileLibrary.Load(options);
        var second = TileLibrary.Load(options);
        ImageCodec.Save(PixelBuffer.Solid(20, 20, new Rgb(128, 0, 128)), paths[0]);
        File.SetLastWriteTimeUtc(paths[0], DateTime.UtcNow.AddMinutes(1));
        var third = TileLibrary.Load(options);

        Assert.Equal((0, 5), (first.Stats.FromCache, first.Stats.Decoded));
        Assert.Equal((5, 0), (second.Stats.FromCache, second.Stats.Decoded));
        Assert.Equal((4, 1), (third.Stats.FromCache, third.Stats.Decoded));
        Assert.Contains(third.Tiles, t => t.Mean == new Rgb(128, 0, 128));
        Assert.Equal(first.Tiles.Select(t => t.Pixels.Data), second.Tiles.Select(t => t.Pixels.Data));
    }

    [Fact]
    public void CanBeCancelled()
    {
        using var temp = new TempFolder();
        temp.SaveImage("a.png", new PixelBuffer(4, 4));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() =>
            TileLibrary.Load(new TileLibraryOptions(temp.Path, 8) { CacheDirectory = null }, cancellationToken: cts.Token));
    }

    [Fact]
    public void ReportsProgress()
    {
        using var temp = new TempFolder();
        for (var i = 0; i < Colors.Length; i++)
        {
            temp.SaveImage($"t{i}.png", PixelBuffer.Solid(10, 10, Colors[i]));
        }

        var reports = new List<MosaicProgress>();
        var progress = new SynchronousProgress<MosaicProgress>(p =>
        {
            lock (reports)
            {
                reports.Add(p);
            }
        });

        TileLibrary.Load(new TileLibraryOptions(temp.Path, 8) { CacheDirectory = null }, progress);

        Assert.Contains(reports, p => p is { Stage: MosaicStage.LoadingTiles, Done: 5, Total: 5 });
    }

    [Fact]
    public void TileFeaturesFollowTheLayout()
    {
        var tile = new Tile("t", TestImages.TwoTone(12, new Rgb(255, 255, 255), new Rgb(0, 0, 0)), featureGrid: 2);

        Assert.Equal(100, tile.Features[0].L, 0);
        Assert.Equal(100, tile.Features[1].L, 0);
        Assert.Equal(0, tile.Features[2].L, 0);
        Assert.Equal(0, tile.Features[3].L, 0);
    }
}

internal sealed class SynchronousProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
