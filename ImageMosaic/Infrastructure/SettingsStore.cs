using System.Text.Json;
using ImageMosaic.Core;

namespace ImageMosaic.Infrastructure;

/// <summary>Remembers the last used settings in %APPDATA%\ImageMosaic\settings.json.</summary>
internal static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ImageMosaic", "settings.json");

    public static MosaicSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<MosaicSettings>(File.ReadAllText(FilePath), Options) ?? new MosaicSettings();
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Broken or unreadable settings: start with defaults.
        }

        return new MosaicSettings();
    }

    public static void Save(MosaicSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Not being able to remember settings is not worth bothering the user.
        }
    }
}
