using System.Collections.Concurrent;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Tiles;

namespace ImageMosaic.Core.Mosaic;

public sealed record MatchOptions
{
    /// <summary>
    /// A tile is not placed again within this many cells of itself (horizontally, vertically or
    /// diagonally). 0 allows neighbours to repeat. Lowered automatically for small libraries.
    /// </summary>
    public int MinRepeatDistance { get; init; } = 4;

    /// <summary>
    /// How strongly frequently used tiles are avoided, in ΔE (color difference) units per "fair share"
    /// of uses. 0 always takes the closest color; higher values spread the work over more photos.
    /// </summary>
    public double UsagePenalty { get; init; } = 3;

    /// <summary>How many tiles closest in average color are compared in detail for each cell.</summary>
    public int CandidateCount { get; init; } = 48;

    /// <summary>Seed of the order in which cells are filled. Same inputs and seed give the same mosaic.</summary>
    public int Seed { get; init; } = 1;
}

/// <summary>Which tile goes into which cell.</summary>
public sealed class MosaicPlan(int columns, int rows, int[] tileIndices, float[] cellErrors, int repeatDistance)
{
    public int Columns { get; } = columns;

    public int Rows { get; } = rows;

    /// <summary>Index into the tile library for every cell, row by row.</summary>
    public int[] TileIndices { get; } = tileIndices;

    /// <summary>Color difference (RMS ΔE over the feature grid) between each cell and its tile.</summary>
    public float[] CellErrors { get; } = cellErrors;

    /// <summary>The repeat distance actually used (may be lower than requested for small libraries).</summary>
    public int RepeatDistance { get; } = repeatDistance;

    public double MeanError => CellErrors.Length == 0 ? 0 : CellErrors.Average();

    public int UniqueTiles => TileIndices.Distinct().Count();
}

/// <summary>
/// Assigns tiles to cells purely by color statistics: each cell is compared with candidate tiles by
/// the average colors of a small grid of sub-cells, in CIE Lab. Repeats close to each other are
/// forbidden and frequently used tiles get a growing penalty.
/// </summary>
public static class TileMatcher
{
    // Quantization of a cell's mean color used to share candidate lists between similar cells.
    private const float LStep = 3f;
    private const float AbStep = 4f;
    private const int AbBins = 72; // covers a/b in [-144, 144)

    public static MosaicPlan Match(
        TargetGrid grid,
        TileLibrary library,
        MatchOptions options,
        IProgress<MosaicProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var tiles = library.Tiles;
        var tileCount = tiles.Count;
        if (tileCount == 0)
        {
            throw new InvalidOperationException("The tile library is empty.");
        }

        if (grid.FeatureGrid != library.FeatureGrid)
        {
            throw new ArgumentException("Target grid and tile library use different feature grids.", nameof(grid));
        }

        var repeatDistance = Math.Max(0, options.MinRepeatDistance);
        while (repeatDistance > 0 && Square(2 * repeatDistance + 1) > tileCount)
        {
            repeatDistance--;
        }

        var windowSize = Square(2 * repeatDistance + 1) - 1;
        // Enough candidates that some always remain after excluding every neighbour.
        var candidateCount = Math.Min(tileCount, Math.Max(1, options.CandidateCount) + windowSize);

        var cellCount = grid.CellCount;
        var keys = new int[cellCount];
        for (var i = 0; i < cellCount; i++)
        {
            keys[i] = Quantize(grid.MeansLab[i]);
        }

        var candidates = BuildCandidateLists(keys, tiles, candidateCount, cancellationToken);

        var order = Enumerable.Range(0, cellCount).ToArray();
        new Random(options.Seed).Shuffle(order);

        var assigned = new int[cellCount];
        Array.Fill(assigned, -1);
        var errors = new float[cellCount];
        var uses = new int[tileCount];
        var mark = new int[tileCount];
        var stamp = 0;
        var fairShare = Math.Max(1.0, (double)cellCount / tileCount);
        var penaltyPerUse = (float)(Math.Max(0, options.UsagePenalty) / fairShare);
        var featureCount = grid.FeatureGrid * grid.FeatureGrid;
        var counter = new ThrottledProgress(progress, MosaicStage.Matching, cellCount);
        counter.Start();

        for (var step = 0; step < order.Length; step++)
        {
            if ((step & 1023) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var cell = order[step];
            var col = cell % grid.Columns;
            var row = cell / grid.Columns;
            stamp++;
            if (repeatDistance > 0)
            {
                var r0 = Math.Max(0, row - repeatDistance);
                var r1 = Math.Min(grid.Rows - 1, row + repeatDistance);
                var c0 = Math.Max(0, col - repeatDistance);
                var c1 = Math.Min(grid.Columns - 1, col + repeatDistance);
                for (var r = r0; r <= r1; r++)
                {
                    for (var c = c0; c <= c1; c++)
                    {
                        var t = assigned[r * grid.Columns + c];
                        if (t >= 0)
                        {
                            mark[t] = stamp;
                        }
                    }
                }
            }

            var cellFeatures = grid.CellFeatures(cell);
            int best = -1, fallback = -1;
            float bestCost = float.MaxValue, bestError = 0, fallbackCost = float.MaxValue, fallbackError = 0;
            foreach (var t in candidates[keys[cell]])
            {
                var error = FeatureError(cellFeatures, tiles[t].Features, featureCount);
                var cost = error + penaltyPerUse * uses[t];
                if (mark[t] == stamp)
                {
                    if (cost < fallbackCost)
                    {
                        (fallback, fallbackCost, fallbackError) = (t, cost, error);
                    }
                }
                else if (cost < bestCost)
                {
                    (best, bestCost, bestError) = (t, cost, error);
                }
            }

            if (best < 0)
            {
                (best, bestError) = (fallback, fallbackError);
            }

            assigned[cell] = best;
            errors[cell] = bestError;
            uses[best]++;
            counter.Increment();
        }

        return new MosaicPlan(grid.Columns, grid.Rows, assigned, errors, repeatDistance);
    }

    /// <summary>Root mean square ΔE between corresponding sub-cell colors.</summary>
    public static float FeatureError(ReadOnlySpan<Lab> a, ReadOnlySpan<Lab> b, int count)
    {
        var sum = 0f;
        for (var i = 0; i < count; i++)
        {
            sum += ColorMath.DistanceSquared(a[i], b[i]);
        }

        return MathF.Sqrt(sum / count);
    }

    private static Dictionary<int, int[]> BuildCandidateLists(
        int[] keys,
        IReadOnlyList<Tile> tiles,
        int candidateCount,
        CancellationToken cancellationToken)
    {
        var means = tiles.Select(t => t.MeanLab).ToArray();
        var unique = keys.Distinct().ToArray();
        var result = new ConcurrentDictionary<int, int[]>();
        var parallel = new ParallelOptions { CancellationToken = cancellationToken };
        Parallel.ForEach(unique, parallel, key =>
        {
            var center = Dequantize(key);
            // Max-heap (by negated priority) of the closest tiles found so far.
            var heap = new PriorityQueue<int, float>(candidateCount + 1);
            for (var t = 0; t < means.Length; t++)
            {
                var d = ColorMath.DistanceSquared(center, means[t]);
                if (heap.Count < candidateCount)
                {
                    heap.Enqueue(t, -d);
                }
                else if (heap.TryPeek(out _, out var worst) && d < -worst)
                {
                    heap.DequeueEnqueue(t, -d);
                }
            }

            var list = new int[heap.Count];
            for (var i = list.Length - 1; i >= 0; i--)
            {
                list[i] = heap.Dequeue(); // closest ends up first
            }

            result[key] = list;
        });

        return new Dictionary<int, int[]>(result);
    }

    private static int Quantize(Lab lab)
    {
        var l = Math.Clamp((int)(lab.L / LStep), 0, (int)(100 / LStep));
        var a = Math.Clamp((int)Math.Floor(lab.A / AbStep) + AbBins / 2, 0, AbBins - 1);
        var b = Math.Clamp((int)Math.Floor(lab.B / AbStep) + AbBins / 2, 0, AbBins - 1);
        return (l * AbBins + a) * AbBins + b;
    }

    private static Lab Dequantize(int key)
    {
        var b = key % AbBins;
        var a = key / AbBins % AbBins;
        var l = key / (AbBins * AbBins);
        return new Lab(
            (l + 0.5f) * LStep,
            (a - AbBins / 2 + 0.5f) * AbStep,
            (b - AbBins / 2 + 0.5f) * AbStep);
    }

    private static int Square(int x) => x * x;
}
