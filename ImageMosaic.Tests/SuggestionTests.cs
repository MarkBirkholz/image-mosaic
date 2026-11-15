using System.Text.Json;
using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Suggestions;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Tests;

public class SuggestionScoringTests
{
    private static readonly TargetMetrics Good = new(MeanError: 6, Contrast: 22, Detail: 9, ColorVariety: 0.9);

    [Fact]
    public void ScoreRewardsEveryCriterion()
    {
        var weights = SuggestionWeights.Default with { Detail = 15 };
        double Score(TargetMetrics m) => TargetSuggester.Score(m, weights, ScoreScale.Absolute).Score;
        var (best, notes) = TargetSuggester.Score(Good, weights, ScoreScale.Absolute);

        Assert.InRange(best, 90, 100);
        Assert.Contains(SuggestionNote.ColorsWellCovered, notes);
        Assert.True(Score(Good with { MeanError = 20 }) < best);
        Assert.True(Score(Good with { Contrast = 4 }) < best);
        Assert.True(Score(Good with { Detail = 1 }) < best);
        Assert.True(Score(Good with { ColorVariety = 0.1 }) < best);
    }

    [Fact]
    public void NotesExplainWeakPoints()
    {
        var (_, notes) = TargetSuggester.Score(new TargetMetrics(MeanError: 25, Contrast: 3, Detail: 0.5, ColorVariety: 0.05));

        Assert.Equal(
            [SuggestionNote.FewPhotosInTheseColors, SuggestionNote.MostlyOneColor, SuggestionNote.LowContrast, SuggestionNote.LittleDetail],
            notes);
    }

    [Fact]
    public void WeightsDecideTheOrder()
    {
        var colorful = Candidate("colorful.jpg", new TargetMetrics(18, 20, 5, 0.9));
        var gray = Candidate("gray.jpg", new TargetMetrics(5, 20, 5, 0.05));
        var colorsOnly = new SuggestionWeights { ColorMatch = 100, ColorVariety = 0, Contrast = 0, Detail = 0 };
        var varietyOnly = new SuggestionWeights { ColorMatch = 0, ColorVariety = 100, Contrast = 0, Detail = 0 };

        Assert.Equal(["gray.jpg", "colorful.jpg"], TargetSuggester.Rank([colorful, gray], colorsOnly, 10).Select(s => s.Path));
        Assert.Equal(["colorful.jpg", "gray.jpg"], TargetSuggester.Rank([colorful, gray], varietyOnly, 10).Select(s => s.Path));
    }

    [Fact]
    public void OnlyRatiosMatterAndZeroWeightsCountEqually()
    {
        var weights = new SuggestionWeights { ColorMatch = 30, ColorVariety = 20, Contrast = 10, Detail = 5 };
        var doubled = new SuggestionWeights { ColorMatch = 60, ColorVariety = 40, Contrast = 20, Detail = 10 };
        var zero = new SuggestionWeights { ColorMatch = 0, ColorVariety = 0, Contrast = 0, Detail = 0 };
        var equal = new SuggestionWeights { ColorMatch = 25, ColorVariety = 25, Contrast = 25, Detail = 0 };
        var scale = ScoreScale.Absolute;

        Assert.Equal(TargetSuggester.Score(Good, weights, scale).Score, TargetSuggester.Score(Good, doubled, scale).Score);
        Assert.Equal(TargetSuggester.Score(Good, equal, scale).Score, TargetSuggester.Score(Good, zero, scale).Score);
    }

    [Theory]
    [InlineData(100, "p9.jpg")]
    [InlineData(-100, "p0.jpg")]
    public void DetailSliderPrefersRicherOrSimplerPhotos(double detail, string first)
    {
        var candidates = Enumerable.Range(0, 10).Select(i => Candidate($"p{i}.jpg", new TargetMetrics(10, 10, Detail: i, 0.5))).ToList();
        var weights = new SuggestionWeights { ColorMatch = 0, ColorVariety = 0, Contrast = 0, Detail = detail };

        Assert.Equal(first, TargetSuggester.Rank(candidates, weights, 10)[0].Path);
    }

    [Fact]
    public void DetailInTheMiddleIsIgnored()
    {
        var simple = Candidate("simple.jpg", new TargetMetrics(10, 20, Detail: 1, 0.5));
        var rich = Candidate("rich.jpg", new TargetMetrics(10, 20, Detail: 9, 0.5));
        var neutral = SuggestionWeights.Default with { Detail = 0 };
        var allZero = new SuggestionWeights { ColorMatch = 0, ColorVariety = 0, Contrast = 0, Detail = 0 };

        foreach (var weights in new[] { neutral, allZero })
        {
            var scores = TargetSuggester.Rank([simple, rich], weights, 2).Select(s => s.Score).ToList();
            Assert.Equal(scores[0], scores[1]);
        }
    }

    [Fact]
    public void WeightsAreSavedWithTheSettings()
    {
        var settings = new MosaicSettings { SuggestionWeights = SuggestionWeights.Default with { Detail = -55, Contrast = 80 } };

        var restored = JsonSerializer.Deserialize<MosaicSettings>(JsonSerializer.Serialize(settings))!;
        var fromOldFile = JsonSerializer.Deserialize<MosaicSettings>("""{ "SuggestionWeights": { "NoFlatAreas": 15, "LowResolutionPenalty": 40, "DetailLimit": 80 } }""")!;

        Assert.Equal(settings.SuggestionWeights, restored.SuggestionWeights);
        Assert.Equal(SuggestionWeights.Default, fromOldFile.SuggestionWeights);
    }

    internal static TargetCandidate Candidate(string path, TargetMetrics metrics) => new(path, 4000, 3000, metrics);
}

public class ScoreScaleTests
{
    [Fact]
    public void FewPhotosUseFixedThresholds()
    {
        var metrics = Enumerable.Range(0, 5).Select(i => Metrics(error: 10 + i)).ToList();

        Assert.Same(ScoreScale.Absolute, ScoreScale.FromCandidates(metrics));
    }

    [Fact]
    public void CalibrationSpreadsMarksOverTheLibrary()
    {
        // A library inside a narrow band of the fixed thresholds: ΔE 15–24 gives absolute marks 0–0.44 only.
        var metrics = Enumerable.Range(0, 100).Select(i => Metrics(error: 15 + i * 0.09)).ToList();

        var scale = ScoreScale.FromCandidates(metrics);

        Assert.Equal(1, scale.Parts(metrics.MinBy(m => m.MeanError)!).ColorMatch);
        Assert.Equal(0, scale.Parts(metrics.MaxBy(m => m.MeanError)!).ColorMatch);
        Assert.True(ScoreScale.Absolute.Parts(metrics.MinBy(m => m.MeanError)!).ColorMatch < 0.5);
    }

    [Fact]
    public void TinyDifferencesAreNotBlownUp()
    {
        var metrics = Enumerable.Range(0, 50).Select(i => Metrics(contrast: 18 + i * 0.01)).ToList();

        var scale = ScoreScale.FromCandidates(metrics);
        var marks = metrics.Select(m => scale.Parts(m).Contrast).ToList();

        Assert.True(marks.Max() - marks.Min() < 0.2);
    }

    [Fact]
    public void MoreDetailGivesAHigherMark()
    {
        var low = ScoreScale.Absolute.Parts(Metrics(detail: 1)).Detail;
        var high = ScoreScale.Absolute.Parts(Metrics(detail: 8)).Detail;

        Assert.True(high > low);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.5, 5.5)]
    [InlineData(1.0, 10.0)]
    public void PercentileInterpolates(double share, double expected)
    {
        Assert.Equal(expected, ScoreScale.Percentile(Enumerable.Range(1, 10).Select(i => (double)i), share), 6);
    }

    private static TargetMetrics Metrics(double error = 12, double contrast = 15, double detail = 5) =>
        new(MeanError: error, Contrast: contrast, Detail: detail, ColorVariety: 0.5);
}

public class SuggestionAnalysisRealPhotosTests
{
    private static readonly Rgb[] Bright =
    [
        new(220, 40, 40), new(40, 180, 60), new(40, 80, 220), new(240, 200, 40), new(250, 250, 250), new(20, 20, 20),
    ];

    [Fact]
    public void MeasuresFlatAndStructuredImages()
    {
        var library = TileLibrary.FromPixels(Bright.Select(c => PixelBuffer.Solid(8, 8, c)));
        var options = new SuggestionOptions { Columns = 16 };

        var flat = TargetSuggester.Measure(PixelBuffer.Solid(160, 120, new Rgb(128, 128, 128)), library, options);
        var colorful = TargetSuggester.Measure(Checkerboard(160, 120, 15), library, options);

        Assert.Equal(0, flat.Contrast, 3);
        Assert.Equal(0, flat.ColorVariety, 3);
        Assert.Equal(0, flat.Detail, 3);
        Assert.True(colorful.Contrast > 10);
        Assert.True(colorful.ColorVariety > 0.4);
        Assert.True(colorful.Detail > 5);
        Assert.True(colorful.MeanError < flat.MeanError);
    }

    [Fact]
    public void RanksColorfulPhotosFirst()
    {
        using var temp = new TempFolder();
        foreach (var (color, i) in Bright.Select((c, i) => (c, i)))
        {
            temp.SaveImage(Path.Combine("tiles", $"{i}.png"), PixelBuffer.Solid(16, 16, color));
        }

        var colorful = temp.SaveImage(Path.Combine("photos", "colorful.png"), Checkerboard(800, 600, 50));
        var gray = temp.SaveImage(Path.Combine("photos", "gray.png"), PixelBuffer.Solid(800, 600, new Rgb(128, 128, 128)));
        var library = TileLibrary.Load(new TileLibraryOptions(Path.Combine(temp.Path, "tiles"), 8) { CacheDirectory = null });
        var candidates = ImageFileScanner.Scan(Path.Combine(temp.Path, "photos"));

        var suggestions = TargetSuggester.Suggest(library, candidates, new SuggestionOptions { Columns = 16 });

        Assert.Equal([colorful, gray], suggestions.Select(s => s.Path));
        Assert.True(suggestions[0].Score > suggestions[1].Score);
    }

    /// <summary>Squares in the six library colors.</summary>
    private static PixelBuffer Checkerboard(int width, int height, int square)
    {
        var image = new PixelBuffer(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetPixel(x, y, Bright[(x / square + 2 * (y / square)) % Bright.Length]);
            }
        }

        return image;
    }
}
