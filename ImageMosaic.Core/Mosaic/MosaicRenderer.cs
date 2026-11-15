using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Core.Mosaic;

public sealed record RenderOptions
{
    /// <summary>0–1: how far each tile's average color is shifted toward its cell's color (keeps the photo's detail).</summary>
    public double ColorCorrection { get; init; } = 0.4;

    /// <summary>0–1: opacity of the target image laid over the tiles (0 = tiles only, 1 = target only).</summary>
    public double Overlay { get; init; } = 0.2;
}

public static class MosaicRenderer
{
    /// <summary>
    /// Draws the planned tiles into an image of <c>Columns × TileSize</c> by <c>Rows × TileSize</c> pixels.
    /// </summary>
    /// <param name="target">The target image at any resolution; used for the overlay.</param>
    public static PixelBuffer Render(
        MosaicPlan plan,
        TileLibrary library,
        TargetGrid grid,
        PixelBuffer target,
        RenderOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var size = library.TileSize;
        var width = plan.Columns * size;
        var height = plan.Rows * size;
        var output = new PixelBuffer(width, height);
        var outData = output.Data;
        var correction = (float)Math.Clamp(options.ColorCorrection, 0, 1);
        var overlay = (float)Math.Clamp(options.Overlay, 0, 1);

        // Bilinear sampling positions of the target for every output column and row.
        var xs = overlay > 0 ? SamplePositions(target.Width, width) : [];
        var ys = overlay > 0 ? SamplePositions(target.Height, height) : [];
        var targetData = target.Data;
        var targetStride = target.Stride;

        var counter = new ThrottledProgress(progress, MosaicStage.Rendering, plan.Rows);
        counter.Start();
        var parallel = new ParallelOptions { CancellationToken = cancellationToken };
        Parallel.For(0, plan.Rows, parallel, row =>
        {
            for (var col = 0; col < plan.Columns; col++)
            {
                var cell = row * plan.Columns + col;
                var tile = library.Tiles[plan.TileIndices[cell]];
                var mean = grid.Means[cell];
                var shiftB = correction * (mean.B - tile.Mean.B);
                var shiftG = correction * (mean.G - tile.Mean.G);
                var shiftR = correction * (mean.R - tile.Mean.R);
                var tileData = tile.Pixels.Data;

                for (var ty = 0; ty < size; ty++)
                {
                    var y = row * size + ty;
                    var dst = (y * width + col * size) * PixelBuffer.BytesPerPixel;
                    var src = ty * size * PixelBuffer.BytesPerPixel;
                    for (var tx = 0; tx < size; tx++)
                    {
                        var b = tileData[src] + shiftB;
                        var g = tileData[src + 1] + shiftG;
                        var r = tileData[src + 2] + shiftR;
                        if (overlay > 0)
                        {
                            var sx = xs[col * size + tx];
                            var sy = ys[y];
                            var i00 = sy.I0 * targetStride + sx.I0 * 3;
                            var i01 = sy.I0 * targetStride + sx.I1 * 3;
                            var i10 = sy.I1 * targetStride + sx.I0 * 3;
                            var i11 = sy.I1 * targetStride + sx.I1 * 3;
                            float Sample(int ch) =>
                                (targetData[i00 + ch] * (1 - sx.F) + targetData[i01 + ch] * sx.F) * (1 - sy.F) +
                                (targetData[i10 + ch] * (1 - sx.F) + targetData[i11 + ch] * sx.F) * sy.F;
                            b += (Sample(0) - b) * overlay;
                            g += (Sample(1) - g) * overlay;
                            r += (Sample(2) - r) * overlay;
                        }

                        outData[dst] = Rgb.ClampToByte(b);
                        outData[dst + 1] = Rgb.ClampToByte(g);
                        outData[dst + 2] = Rgb.ClampToByte(r);
                        dst += 3;
                        src += 3;
                    }
                }
            }

            counter.Increment();
        });

        return output;
    }

    private readonly record struct Sample1D(int I0, int I1, float F);

    private static Sample1D[] SamplePositions(int source, int destination)
    {
        var result = new Sample1D[destination];
        var scale = (double)source / destination;
        for (var i = 0; i < destination; i++)
        {
            var x = Math.Clamp((i + 0.5) * scale - 0.5, 0, source - 1);
            var x0 = (int)x;
            result[i] = new Sample1D(x0, Math.Min(x0 + 1, source - 1), (float)(x - x0));
        }

        return result;
    }
}
