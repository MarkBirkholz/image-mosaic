using System.Collections.Concurrent;
using ImageMosaic.Core.Imaging;

namespace ImageMosaic.Core.Tiles;

/// <summary>
/// Photos prepared at bigger tile sizes for fragments, kept while a frame is being chosen: moving the frame
/// or making it bigger reuses the photos already read, and a smaller tile size is made from a bigger copy
/// without reading the photo again. Each photo keeps only its biggest copy.
/// </summary>
public sealed class FragmentTileCache(long maxBytes = FragmentTileCache.DefaultMaxBytes)
{
    public const long DefaultMaxBytes = 512L * 1024 * 1024;

    private readonly ConcurrentDictionary<string, PixelBuffer> tiles = new(StringComparer.OrdinalIgnoreCase);
    private long bytes;
    private int loaded;

    /// <summary>How many times a photo was read from disk.</summary>
    public int Loaded => loaded;

    /// <summary>The photo as a <paramref name="tileSize"/> tile.</summary>
    public PixelBuffer Get(string path, int tileSize)
    {
        if (tiles.TryGetValue(path, out var cached) && cached.Width >= tileSize)
        {
            return cached.Width == tileSize ? cached : cached.Resize(tileSize, tileSize);
        }

        var tile = TileLibrary.PrepareTile(path, tileSize);
        Interlocked.Increment(ref loaded);
        if (Interlocked.Read(ref bytes) + tile.Data.Length > maxBytes)
        {
            Clear();
        }

        tiles.AddOrUpdate(
            path,
            _ =>
            {
                Interlocked.Add(ref bytes, tile.Data.Length);
                return tile;
            },
            (_, old) =>
            {
                if (old.Width >= tile.Width)
                {
                    return old;
                }

                Interlocked.Add(ref bytes, tile.Data.Length - old.Data.Length);
                return tile;
            });
        return tile;
    }

    public void Clear()
    {
        tiles.Clear();
        Interlocked.Exchange(ref bytes, 0);
    }
}
