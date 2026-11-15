using ImageMosaic.Core.Imaging;

namespace ImageMosaic.Core.Mosaic;

/// <summary>The target image cut into <see cref="Columns"/> × <see cref="Rows"/> cells, with the same color signature as tiles.</summary>
public sealed class TargetGrid
{
    private TargetGrid(int columns, int rows, int featureGrid, Lab[] features, Rgb[] means, Lab[] meansLab)
    {
        Columns = columns;
        Rows = rows;
        FeatureGrid = featureGrid;
        Features = features;
        Means = means;
        MeansLab = meansLab;
    }

    public int Columns { get; }

    public int Rows { get; }

    public int CellCount => Columns * Rows;

    public int FeatureGrid { get; }

    /// <summary><c>FeatureGrid²</c> colors per cell, cells row by row.</summary>
    public Lab[] Features { get; }

    public Rgb[] Means { get; }

    public Lab[] MeansLab { get; }

    public ReadOnlySpan<Lab> CellFeatures(int cell)
    {
        var n = FeatureGrid * FeatureGrid;
        return Features.AsSpan(cell * n, n);
    }

    /// <summary>Number of rows that keeps the image's aspect ratio with square cells.</summary>
    public static int RowsFor(int columns, int imageWidth, int imageHeight) =>
        Math.Max(1, (int)Math.Round((double)columns * imageHeight / imageWidth));

    public static TargetGrid FromImage(PixelBuffer image, int columns, int rows, int featureGrid)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(featureGrid, 1);

        // One pixel of this image is exactly the average of one sub-cell.
        var small = image.Resize(columns * featureGrid, rows * featureGrid);
        var n = featureGrid * featureGrid;
        var features = new Lab[columns * rows * n];
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < columns; col++)
            {
                var offset = (row * columns + col) * n;
                for (var sy = 0; sy < featureGrid; sy++)
                {
                    for (var sx = 0; sx < featureGrid; sx++)
                    {
                        var c = small.GetPixel(col * featureGrid + sx, row * featureGrid + sy);
                        features[offset + sy * featureGrid + sx] = ColorMath.ToLab(c);
                    }
                }
            }
        }

        var cells = image.Resize(columns, rows);
        var means = new Rgb[columns * rows];
        var meansLab = new Lab[columns * rows];
        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < columns; col++)
            {
                var c = cells.GetPixel(col, row);
                means[row * columns + col] = c;
                meansLab[row * columns + col] = ColorMath.ToLab(c);
            }
        }

        return new TargetGrid(columns, rows, featureGrid, features, means, meansLab);
    }
}
