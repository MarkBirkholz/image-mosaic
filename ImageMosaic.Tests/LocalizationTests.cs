using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using ImageMosaic.Core;
using ImageMosaic.Core.Localization;

namespace ImageMosaic.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void UseEnglish() => Lang.Use("en");
}

// The language is global, so these tests must not run in parallel with the others.
[CollectionDefinition(nameof(LanguageSwitching), DisableParallelization = true)]
public class LanguageSwitching;

[Collection(nameof(LanguageSwitching))]
public class LocalizationTests
{
    private static readonly string Folder = Path.Combine(AppContext.BaseDirectory, "Languages");

    [Fact]
    public void EveryFileIsALanguage()
    {
        Assert.Equal(
            Directory.GetFiles(Folder, "*.json").Select(Path.GetFileNameWithoutExtension).Order(),
            Lang.Languages.Select(l => l.Code).Order());
    }

    [Fact]
    public void SwitchesLanguage()
    {
        try
        {
            Lang.Use("ru");
            Assert.Equal("Собрать мозаику", T(Texts.Main.Build));
            Assert.StartsWith("Подбор", new MosaicProgress(MosaicStage.Matching, 5, 10).ToString());
            Assert.Contains("Выберите целевую картинку.", new MosaicSettings().Validate());
        }
        finally
        {
            Lang.Use("en");
        }

        Assert.Equal("Build mosaic", T(Texts.Main.Build));
    }

    [Fact]
    public void NewFileAddsALanguageAndMissingTextsAreEnglish()
    {
        using var temp = new TempFolder();
        File.Copy(Path.Combine(Folder, "en.json"), temp.File("en.json"));
        File.WriteAllText(temp.File("de.json"), """{ "name": "Deutsch", "strings": { "main.build": "Mosaik erstellen" } }""");
        try
        {
            Lang.Load(temp.Path);
            Lang.Use("de");

            Assert.Contains(new Language("de", "Deutsch"), Lang.Languages);
            Assert.Equal("Mosaik erstellen", T(Texts.Main.Build));
            Assert.Equal("Save…", T(Texts.Main.Save));
        }
        finally
        {
            Lang.Load(Folder);
            Lang.Use("en");
        }
    }

    [Fact]
    public void UnknownLanguageFallsBack()
    {
        Lang.Use("xx");
        Assert.Contains(Lang.Current, Lang.Languages.Select(l => l.Code));
        Lang.Use("en");
    }

    [Fact]
    public void TextsAndEnglishFileHaveTheSameKeys()
    {
        var constants = typeof(Texts).GetNestedTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Select(f => (string)f.GetRawConstantValue()!);

        Assert.Equal(Strings("en").Keys.Order(), constants.Order());
    }

    [Fact]
    public void EveryLanguageHasAllTextsWithTheSamePlaceholders()
    {
        var english = Strings("en");
        foreach (var code in Lang.Languages.Select(l => l.Code))
        {
            var strings = Strings(code);
            Assert.Equal(english.Keys.Order(), strings.Keys.Order());
            foreach (var (key, text) in english)
            {
                Assert.True(Placeholders(text).SetEquals(Placeholders(strings[key])), $"{code}: {key}");
            }
        }
    }

    private static Dictionary<string, string> Strings(string code)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Folder, code + ".json")));
        return json.RootElement.GetProperty("strings").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
    }

    private static HashSet<string> Placeholders(string text) =>
        Regex.Matches(text, @"\{\d+(:[^}]*)?\}").Select(m => m.Value).ToHashSet();
}
