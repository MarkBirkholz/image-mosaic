using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Core.Mosaic;

public sealed record RenderOptions
{
    /// <summary>0–1: how far each tile's average color is shifted toward its cell's color (keeps the photo's detail).</summary>
    public double ColorCorrection { get; init; } = 0.4;

    /// <summary>0–1: opacity of the target image laid over the tiles (0 = tiles only, 1 = target only).</summary>
    public double Overlay { get; init; } = 0.2;

    /// <summary>
    /// 0–1: width of the soft transition between neighbouring tiles, as a share of the tile size.
    /// 0 = sharp edges; 1 = each tile fades into its neighbours from its center.
    /// </summary>
    public double EdgeBlend { get; init; }
}

public static class MosaicRenderer
{
    /// <summary>
    /// Draws the planned tiles into an image of <c>Columns × TileSize</c> by <c>Rows × TileSize</c> pixels.
    /// </summary>
    /// <param name="target">The target image at any resolution; used for the overlay.</param>
    /// <param name="targetRegion">The part of <paramref name="target"/> the plan covers, in target pixels; the whole target when null.</param>
    public static PixelBuffer Render(
        MosaicPlan plan,
        TileLibrary library,
        TargetGrid grid,
        PixelBuffer target,
        RenderOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default,
        TargetRegion? targetRegion = null)
    {
        var region = targetRegion ?? new TargetRegion(0, 0, target.Width, target.Height);
        var size = library.TileSize;
        var width = plan.Columns * size;
        var height = plan.Rows * size;
        var output = new PixelBuffer(width, height);
        var correction = (float)Math.Clamp(options.ColorCorrection, 0, 1);
        var overlay = (float)Math.Clamp(options.Overlay, 0, 1);
        var halfBlend = (float)(Math.Clamp(options.EdgeBlend, 0, 1) * size / 2);

        // Per cell: the tile's pixels and the color shift toward the cell's average.
        var cells = new CellPaint[plan.TileIndices.Length];
        for (var cell = 0; cell < cells.Length; cell++)
        {
            var tile = library.Tiles[plan.TileIndices[cell]];
            var mean = grid.Means[cell];
            cells[cell] = new CellPaint(
                tile.Pixels.Data,
                correction * (mean.B - tile.Mean.B),
                correction * (mean.G - tile.Mean.G),
                correction * (mean.R - tile.Mean.R));
        }

        var xs = AxisSamples(plan.Columns, size, halfBlend);
        var ys = AxisSamples(plan.Rows, size, halfBlend);
        var targetX = overlay > 0 ? TargetPositions(target.Width, region.X, region.Width, width) : [];
        var targetY = overlay > 0 ? TargetPositions(target.Height, region.Y, region.Height, height) : [];

        var counter = new ThrottledProgress(progress, MosaicStage.Rendering, height);
        counter.Start();
        var parallel = new ParallelOptions { CancellationToken = cancellationToken };
        var outData = output.Data;
        var targetData = target.Data;
        var targetStride = target.Stride;
        var stride = size * PixelBuffer.BytesPerPixel;
        Parallel.For(0, height, parallel, y =>
        {
            var sy = ys[y];
            var di = y * width * PixelBuffer.BytesPerPixel;
            for (var x = 0; x < width; x++, di += 3)
            {
                var sx = xs[x];
                float b = 0, g = 0, r = 0;
                // Up to four tiles contribute near a corner; inside a tile only its own.
                for (var j = 0; j < 2; j++)
                {
                    var wy = j == 0 ? sy.Weight0 : sy.Weight1;
                    if (wy <= 0)
                    {
                        continue;
                    }

                    var row = j == 0 ? sy.Cell0 : sy.Cell1;
                    var ly = j == 0 ? sy.Local0 : sy.Local1;
                    for (var i = 0; i < 2; i++)
                    {
                        var wx = i == 0 ? sx.Weight0 : sx.Weight1;
                        if (wx <= 0)
                        {
                            continue;
                        }

                        var paint = cells[row * plan.Columns + (i == 0 ? sx.Cell0 : sx.Cell1)];
                        var si = ly * stride + (i == 0 ? sx.Local0 : sx.Local1) * 3;
                        var w = wx * wy;
                        b += (paint.Pixels[si] + paint.ShiftB) * w;
                        g += (paint.Pixels[si + 1] + paint.ShiftG) * w;
                        r += (paint.Pixels[si + 2] + paint.ShiftR) * w;
                    }
                }

                if (overlay > 0)
                {
                    var tx = targetX[x];
                    var ty = targetY[y];
                    var i00 = ty.I0 * targetStride + tx.I0 * 3;
                    var i01 = ty.I0 * targetStride + tx.I1 * 3;
                    var i10 = ty.I1 * targetStride + tx.I0 * 3;
                    var i11 = ty.I1 * targetStride + tx.I1 * 3;
                    float Sample(int ch) =>
                        (targetData[i00 + ch] * (1 - tx.F) + targetData[i01 + ch] * tx.F) * (1 - ty.F) +
                        (targetData[i10 + ch] * (1 - tx.F) + targetData[i11 + ch] * tx.F) * ty.F;
                    b += (Sample(0) - b) * overlay;
                    g += (Sample(1) - g) * overlay;
                    r += (Sample(2) - r) * overlay;
                }

                outData[di] = Rgb.ClampToByte(b);
                outData[di + 1] = Rgb.ClampToByte(g);
                outData[di + 2] = Rgb.ClampToByte(r);
            }

            counter.Increment();
        });

        return output;
    }

    private readonly record struct CellPaint(byte[] Pixels, float ShiftB, float ShiftG, float ShiftR);

    /// <summary>Which tile column (or row) a pixel takes its color from, at which pixel inside the tile, and with which weight.</summary>
    private readonly record struct AxisSample(int Cell0, int Local0, float Weight0, int Cell1, int Local1, float Weight1);

    /// <summary>
    /// For every output pixel along one axis: its own tile, and near an edge also the neighbouring tile.
    /// Weights ramp linearly across a band of <c>2 × halfBlend</c> pixels around each seam and always sum to 1.
    /// Pixels a tile needs beyond its own square are mirrored from inside it.
    /// </summary>
    private static AxisSample[] AxisSamples(int cells, int size, float halfBlend)
    {
        var samples = new AxisSample[cells * size];
        for (var p = 0; p < samples.Length; p++)
        {
            var cell = p / size;
            var local = p % size;
            var center = local + 0.5f; // pixel center inside the tile
            if (halfBlend <= 0)
            {
                samples[p] = new AxisSample(cell, local, 1, cell, local, 0);
                continue;
            }

            // Distance from the nearer seam: negative = toward the previous tile.
            var toStart = center;
            var toEnd = size - center;
            if (toStart < halfBlend && cell > 0)
            {
                var own = 0.5f + toStart / (2 * halfBlend);
                samples[p] = new AxisSample(cell, local, own, cell - 1, Mirror(local + size, size), 1 - own);
            }
            else if (toEnd < halfBlend && cell < cells - 1)
            {
                var own = 0.5f + toEnd / (2 * halfBlend);
                samples[p] = new AxisSample(cell, local, own, cell + 1, Mirror(local - size, size), 1 - own);
            }
            else
            {
                samples[p] = new AxisSample(cell, local, 1, cell, local, 0);
            }
        }

        return samples;
    }

    /// <summary>A coordinate outside <c>[0, size)</c> reflected back inside, so a tile continues smoothly past its edge.</summary>
    private static int Mirror(int local, int size)
    {
        if (local < 0)
        {
            local = -local - 1;
        }
        else if (local >= size)
        {
            local = 2 * size - local - 1;
        }

        return Math.Clamp(local, 0, size - 1);
    }

    private readonly record struct Sample1D(int I0, int I1, float F);

    /// <summary>Bilinear sample positions in the target for each output pixel along one axis, within <c>[start, start + length)</c>.</summary>
    private static Sample1D[] TargetPositions(int source, double start, double length, int destination)
    {
        var result = new Sample1D[destination];
        var scale = length / destination;
        for (var i = 0; i < destination; i++)
        {
            var x = Math.Clamp(start + (i + 0.5) * scale - 0.5, 0, source - 1);
            var x0 = (int)x;
            result[i] = new Sample1D(x0, Math.Min(x0 + 1, source - 1), (float)(x - x0));
        }

        return result;
    }
}

/// <summary>A rectangle of the target picture in (fractional) target pixels.</summary>
public readonly record struct TargetRegion(double X, double Y, double Width, double Height);
