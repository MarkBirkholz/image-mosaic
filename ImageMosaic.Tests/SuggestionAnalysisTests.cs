using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Mosaic;
using ImageMosaic.Core.Suggestions;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Tests;

public class SuggestionAnalysisTests
{
    private static readonly SuggestionOptions Options = new() { Columns = 12 };

    [Fact]
    public void ReusedWhileNothingChanged()
    {
        using var temp = Library(out var load);
        var first = SuggestionAnalysis.Run(load(), Options);

        var again = SuggestionAnalysis.Reuse(first, load(), Options);
        var otherWeights = SuggestionAnalysis.Reuse(first, load(), Options with { Weights = new SuggestionWeights { Detail = 90 }, Count = 3 });

        Assert.Same(first, again);
        Assert.Same(first, otherWeights);
        Assert.Equal(4, first.Candidates.Count);
    }

    [Fact]
    public void RedoneWhenAPhotoChanges()
    {
        using var temp = Library(out var load);
        var first = SuggestionAnalysis.Run(load(), Options);
        var photo = Path.Combine(temp.Path, "p0.png");
        ImageCodec.Save(TestImages.Gradient(60, 40), photo);
        File.SetLastWriteTimeUtc(photo, DateTime.UtcNow.AddMinutes(1));

        var next = SuggestionAnalysis.Reuse(first, load(), Options);

        Assert.NotSame(first, next);
    }

    [Fact]
    public void RedoneWhenAPhotoIsAddedOrRemoved()
    {
        using var temp = Library(out var load);
        var first = SuggestionAnalysis.Run(load(), Options);

        temp.SaveImage("new.png", PixelBuffer.Solid(60, 40, new Rgb(9, 99, 199)));
        var added = SuggestionAnalysis.Reuse(first, load(), Options);
        File.Delete(Path.Combine(temp.Path, "new.png"));
        File.Delete(Path.Combine(temp.Path, "p1.png"));
        var removed = SuggestionAnalysis.Reuse(added, load(), Options);

        Assert.NotSame(first, added);
        Assert.Equal(5, added.Candidates.Count);
        Assert.NotSame(added, removed);
        Assert.Equal(3, removed.Candidates.Count);
    }

    [Fact]
    public void RedoneWhenMatchingOrTileSettingsChange()
    {
        using var temp = Library(out var load);
        var library = load();
        var first = SuggestionAnalysis.Run(library, Options);
        var otherTiles = TileLibrary.Load(new TileLibraryOptions(temp.Path, 12) { CacheDirectory = null });

        Assert.False(first.IsValidFor(library, Options with { Match = new MatchOptions { MinRepeatDistance = 1 } }));
        Assert.False(first.IsValidFor(library, Options with { Columns = 20 }));
        Assert.False(first.IsValidFor(otherTiles, Options));
        Assert.True(first.IsValidFor(library, Options));
    }

    private static TempFolder Library(out Func<TileLibrary> load)
    {
        var temp = new TempFolder();
        var colors = new[] { new Rgb(220, 40, 40), new Rgb(40, 180, 60), new Rgb(40, 80, 220), new Rgb(240, 200, 40) };
        for (var i = 0; i < colors.Length; i++)
        {
            temp.SaveImage($"p{i}.png", TestImages.TwoTone(60, colors[i], colors[(i + 1) % colors.Length]).Resize(60, 40));
        }

        var path = temp.Path;
        load = () => TileLibrary.Load(new TileLibraryOptions(path, 8) { CacheDirectory = null });
        return temp;
    }
}
