using System.Globalization;
using System.Text.Json;

namespace ImageMosaic.Core.Localization;

public sealed record Language(string Code, string Name);

/// <summary>
/// Texts from <c>Languages/&lt;code&gt;.json</c>: <c>{ "name": "Русский", "strings": { "main.build": "…" } }</c>.
/// Every file is a language; missing texts fall back to the default language.
/// </summary>
public static class Lang
{
    private const string DefaultCode = "en";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static Dictionary<string, LanguageFile> files = Read(Path.Combine(AppContext.BaseDirectory, "Languages"));
    private static LanguageFile? current;

    public static IReadOnlyList<Language> Languages =>
        files.Select(f => new Language(f.Key, f.Value.Name)).OrderBy(l => l.Name, StringComparer.CurrentCulture).ToList();

    public static string Current { get; private set; } = DefaultCode;

    public static void Load(string directory) => files = Read(directory);

    /// <summary>Switches to <paramref name="code"/>; if there is no such file, to the system language or the default.</summary>
    public static void Use(string? code)
    {
        var system = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        Current = new[] { code, system }.FirstOrDefault(c => c != null && files.ContainsKey(c)) ?? DefaultCode;
        current = files.GetValueOrDefault(Current);
    }

    public static string T(string key) =>
        current?.Strings.GetValueOrDefault(key)
        ?? files.GetValueOrDefault(DefaultCode)?.Strings.GetValueOrDefault(key)
        ?? key;

    public static string TF(string key, params object?[] args) => string.Format(CultureInfo.CurrentCulture, T(key), args);

    private static Dictionary<string, LanguageFile> Read(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.json").ToDictionary(
                f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant(),
                f => JsonSerializer.Deserialize<LanguageFile>(File.ReadAllText(f), JsonOptions)!)
            : [];


    private sealed record LanguageFile(string Name, Dictionary<string, string> Strings);
}
