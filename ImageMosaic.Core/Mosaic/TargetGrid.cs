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

    /// <summary>The cells of a rectangle of this grid, as a grid of its own.</summary>
    public TargetGrid Region(CellRect rect)
    {
        var n = FeatureGrid * FeatureGrid;
        var count = rect.Columns * rect.Rows;
        var features = new Lab[count * n];
        var means = new Rgb[count];
        var meansLab = new Lab[count];
        for (var r = 0; r < rect.Rows; r++)
        {
            for (var c = 0; c < rect.Columns; c++)
            {
                var from = (rect.Row + r) * Columns + rect.Column + c;
                var to = r * rect.Columns + c;
                Array.Copy(Features, from * n, features, to * n, n);
                means[to] = Means[from];
                meansLab[to] = MeansLab[from];
            }
        }

        return new TargetGrid(rect.Columns, rect.Rows, FeatureGrid, features, means, meansLab);
    }

    /// <summary>
    /// Columns and rows of square cells for an image, with <paramref name="longSide"/> cells along its longer
    /// side, so landscape and portrait pictures get the same detail.
    /// </summary>
    public static (int Columns, int Rows) SizeFor(int longSide, int imageWidth, int imageHeight)
    {
        var shortSide = Math.Max(1, (int)Math.Round((double)longSide * Math.Min(imageWidth, imageHeight) / Math.Max(imageWidth, imageHeight)));
        return imageWidth >= imageHeight ? (longSide, shortSide) : (shortSide, longSide);
    }

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
