using ImageMosaic.Core.Imaging;

namespace ImageMosaic.Core.Tiles;

/// <summary>A source photo cut to a square of the tile size, with its color signature.</summary>
public sealed class Tile
{
    public Tile(string sourcePath, PixelBuffer pixels, int featureGrid)
    {
        SourcePath = sourcePath;
        Pixels = pixels;
        Mean = pixels.Average();
        MeanLab = ColorMath.ToLab(Mean);
        Features = ComputeFeatures(pixels, featureGrid);
    }

    public string SourcePath { get; }

    public PixelBuffer Pixels { get; }

    public Rgb Mean { get; }

    public Lab MeanLab { get; }

    /// <summary>
    /// Average color of each cell of a <c>grid × grid</c> split of the tile, row by row.
    /// Matching these instead of a single average keeps the rough layout of light and dark areas.
    /// </summary>
    public Lab[] Features { get; }

    public static Lab[] ComputeFeatures(PixelBuffer pixels, int grid)
    {
        var small = pixels.Resize(grid, grid);
        var features = new Lab[grid * grid];
        for (var y = 0; y < grid; y++)
        {
            for (var x = 0; x < grid; x++)
            {
                features[y * grid + x] = ColorMath.ToLab(small.GetPixel(x, y));
            }
        }

        return features;
    }
}
