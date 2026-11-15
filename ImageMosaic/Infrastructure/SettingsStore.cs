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
                return settings.TilesFolders.Count > 0 ? settings : settings with { TilesFolders = LegacyFolder(json) };
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Broken or unreadable settings: start with defaults.
        }

        return new MosaicSettings();
    }

    /// <summary>Settings from version 2.0 had a single "TilesFolder" string.</summary>
    private static List<string> LegacyFolder(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("TilesFolder", out var folder) &&
               folder.ValueKind == JsonValueKind.String &&
               !string.IsNullOrWhiteSpace(folder.GetString())
            ? [folder.GetString()!]
            : [];
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
