using System.Globalization;
using ImageMosaic.Core;
using ImageMosaic.Core.Imaging;
using ImageMosaic.Core.Localization;
using ImageMosaic.Core.Tiles;

const string Usage = """
    Builds a photo mosaic from the command line.

    Usage:
      mosaic --target <image> --tiles <folder> [--tiles <folder>...] --out <file.jpg|png> [options]

    Several tile folders can be given by repeating --tiles or separating them with ";".

    Options:
      --long-side <n>     tiles along the longer side of the target (default 80)
      --tile-size <px>    size of one tile in the output (default 32)
      --grid <n>          colors compared per tile side, 1-8 (default 3)
      --repeat <n>        min distance between equal tiles, in cells (default 4)
      --penalty <x>       penalty for reusing a photo, ΔE per fair share (default 3)
      --correction <0-1>  shift tile colors toward the target (default 0.4)
      --overlay <0-1>     blend the target over the tiles (default 0.2)
      --blend <0-0.25>    soft transition between tiles, share of a tile (default 0)
      --seed <n>          random seed for the fill order (default 1)
      --quality <1-100>   JPEG quality (default 92)
      --cache <folder>    tile cache folder (default: %LOCALAPPDATA%\ImageMosaic\TileCache)
      --no-cache          do not read or write the tile cache
      --lang <code>       language of messages: any file in the Languages folder (default: system language)
    """;

Console.OutputEncoding = System.Text.Encoding.UTF8;

try
{
    var options = ParseArguments(args);
    if (options.Count == 0 || options.ContainsKey("help"))
    {
        Console.WriteLine(Usage);
        return 0;
    }

    Lang.Use(options.GetValueOrDefault("lang"));
    var defaults = new MosaicSettings();
    var settings = new MosaicSettings
    {
        TargetImagePath = Required(options, "target"),
        TilesFolders = Required(options, "tiles").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        LongSideTiles = Int(options, "long-side", defaults.LongSideTiles),
        TileSize = Int(options, "tile-size", defaults.TileSize),
        FeatureGrid = Int(options, "grid", defaults.FeatureGrid),
        MinRepeatDistance = Int(options, "repeat", defaults.MinRepeatDistance),
        UsagePenalty = Double(options, "penalty", defaults.UsagePenalty),
        ColorCorrection = Double(options, "correction", defaults.ColorCorrection),
        Overlay = Double(options, "overlay", defaults.Overlay),
        EdgeBlend = Double(options, "blend", defaults.EdgeBlend),
        Seed = Int(options, "seed", defaults.Seed),
    };
    var output = Required(options, "out");
    var quality = Int(options, "quality", 92);
    var cache = options.ContainsKey("no-cache")
        ? null
        : options.GetValueOrDefault("cache") ?? TileCache.DefaultDirectory;

    var progress = new ConsoleProgress();
    var builder = new MosaicBuilder(cache, message => Console.WriteLine(message));
    var result = builder.Build(settings, progress);
    ImageCodec.Save(result.Image, output, quality);
    Console.WriteLine($"Saved {Path.GetFullPath(output)}");
    return 0;
}
catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or NotSupportedException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(e.Message);
    return 1;
}

static Dictionary<string, string> ParseArguments(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (arg is "-h" or "/?")
        {
            result["help"] = "";
            continue;
        }

        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unexpected argument '{arg}'. Run with --help for usage.");
        }

        var name = arg[2..];
        var isFlag = name is "no-cache" or "help";
        var value = isFlag ? "" : i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for {arg}.");
        // --tiles may be repeated; the folders are collected into one ";"-separated list.
        result[name] = name == "tiles" && result.TryGetValue(name, out var previous) ? previous + ";" + value : value;
    }

    return result;
}

static string Required(Dictionary<string, string> options, string name) =>
    options.TryGetValue(name, out var value) ? value : throw new ArgumentException($"--{name} is required. Run with --help for usage.");

static int Int(Dictionary<string, string> options, string name, int fallback) =>
    !options.TryGetValue(name, out var value) ? fallback
    : int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n
    : throw new ArgumentException($"--{name} expects a whole number, got '{value}'.");

static double Double(Dictionary<string, string> options, string name, double fallback) =>
    !options.TryGetValue(name, out var value) ? fallback
    : double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x
    : throw new ArgumentException($"--{name} expects a number, got '{value}'.");

internal sealed class ConsoleProgress : IProgress<MosaicProgress>
{
    private readonly bool interactive = !Console.IsOutputRedirected;
    private MosaicStage? last;

    public void Report(MosaicProgress value)
    {
        if (!interactive)
        {
            return;
        }

        lock (this)
        {
            if (last != value.Stage && last != null)
            {
                Console.Write("\r" + new string(' ', 60) + "\r");
            }

            last = value.Stage;
            if (value.Stage != MosaicStage.Done)
            {
                Console.Write($"\r{value,-60}");
            }
        }
    }
}
