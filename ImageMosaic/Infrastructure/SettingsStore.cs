using System.Text.Json;
using ImageMosaic.Core;

namespace ImageMosaic.Infrastructure;

/// <summary>Remembers the last used settings, except the target picture, in %APPDATA%\ImageMosaic\settings.json.</summary>
internal static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>The settings file, or the one in the IMAGEMOSAIC_SETTINGS environment variable (used by test runs).</summary>
    private static string FilePath =>
        Environment.GetEnvironmentVariable("IMAGEMOSAIC_SETTINGS") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImageMosaic", "settings.json");

    public static MosaicSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var settings = JsonSerializer.Deserialize<MosaicSettings>(json, Options) ?? new MosaicSettings();
                using var document = JsonDocument.Parse(json);
                return FromOlderVersions(settings, document.RootElement);
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Broken or unreadable settings: start with defaults.
        }

        return new MosaicSettings();
    }

    /// <summary>Older settings files had a single "TilesFolder" string and "Columns", tiles across the width.</summary>
    private static MosaicSettings FromOlderVersions(MosaicSettings settings, JsonElement root)
    {
        if (settings.TilesFolders.Count == 0 &&
            root.TryGetProperty("TilesFolder", out var folder) &&
            folder.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(folder.GetString()))
        {
            settings = settings with { TilesFolders = [folder.GetString()!] };
        }

        if (!root.TryGetProperty(nameof(MosaicSettings.LongSideTiles), out _) &&
            root.TryGetProperty("Columns", out var columns) &&
            columns.TryGetInt32(out var count))
        {
            settings = settings with { LongSideTiles = count };
        }

        return settings;
    }

    public static void Save(MosaicSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings with { TargetImagePath = "" }, Options));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Not being able to remember settings is not worth bothering the user.
        }
    }
}
