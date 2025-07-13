using pk3DS.Core;
using pk3DS.Core.CTR;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PCTRGen6Tools;

internal partial class Helper
{
    [GeneratedRegex(@"^(?:\[[^\[\]]+\]|\\n|\\r|\\c)+$")]
    private static partial Regex TrashPattern();

    public static void Export(string inputRoot, string outputRoot)
    {
        foreach (var filePath in Directory.GetFiles(inputRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(inputRoot, filePath);
            var output = Path.Combine(outputRoot, relativePath);
            Directory.CreateDirectory(output);

            GARC.LazyGARC? garc;
            try
            {
                garc = new GARC.LazyGARC(File.ReadAllBytes(filePath));
                if (garc.FileCount == 0)
                {
                    Console.WriteLine($"Skipping {filePath}: no files found.");
                    continue;
                }
            }
            catch
            {
                Console.WriteLine($"Skipping {filePath}: not a valid GARC file or cannot be read.");
                continue;
            }

            for (var i = 0; i < garc.FileCount; i++)
            {
                File.WriteAllBytes(Path.Combine(output, $"{i:D4}.bin"), garc[i]);
            }
        }
    }

    public static void Import(string inputRoot, string replaceRoot, string outputRoot)
    {
        foreach (var filePath in Directory.GetFiles(inputRoot, "*", SearchOption.AllDirectories))
        {
            var replaced = false;
            var relativePath = Path.GetRelativePath(inputRoot, filePath);

            GARC.LazyGARC? garc;
            try
            {
                garc = new GARC.LazyGARC(File.ReadAllBytes(filePath));
                if (garc.FileCount == 0)
                {
                    Console.WriteLine($"Skipping {filePath}: no files found.");
                    continue;
                }
            }
            catch
            {
                Console.WriteLine($"Skipping {filePath}: not a valid GARC file or cannot be read.");
                continue;
            }

            for (var i = 0; i < garc.FileCount; i++)
            {
                var replace = Path.Combine(replaceRoot, relativePath, $"{i:D4}.bin");
                if (File.Exists(replace))
                {
                    garc[i] = File.ReadAllBytes(replace);
                    replaced = true;
                }
            }

            if (replaced)
            {
                var replaceOutput = Path.Combine(outputRoot, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(replaceOutput)!);
                File.WriteAllBytes(replaceOutput, garc.Save());
            }
        }
    }

    public static void ExportText(string inputRoot, string outputRoot, string? originalRoot = null, string? keyPrefix = null)
    {
        keyPrefix ??= "";

        var UTF8NoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        var gameConfig = new GameConfig(GameVersion.Invalid);
        var original = !string.IsNullOrEmpty(originalRoot);

        Directory.CreateDirectory(outputRoot);
        foreach (var filePath in Directory.GetFiles(inputRoot, "*.bin"))
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var relativePath = Path.GetRelativePath(inputRoot, filePath);

            List<string?>? originalLines = [];
            if (original)
            {
                var originalPath = Path.Combine(originalRoot!, relativePath);
                if (File.Exists(originalPath))
                {
                    var originalFile = new TextFile(gameConfig, File.ReadAllBytes(originalPath), false);
                    originalLines = [.. originalFile.LineData.Select(TextConverter.GetLineString)];
                }
            }

            var textFile = new TextFile(gameConfig, File.ReadAllBytes(filePath), false);
            var translations = textFile.LineData.Select((data, index) =>
            {
                var text = TextConverter.GetLineString(data);
                var item = new TranslationItem
                {
                    Index = index,
                    Key = $"{keyPrefix}{fileName}_{index:d04}",
                    Original = index < originalLines.Count ? originalLines[index] : null,
                    Translation = text,
                };
                if (string.IsNullOrEmpty(item.Original) || TrashPattern().IsMatch(item.Original))
                {
                    item.Trash = true;
                }

                return item;
            }).ToList();

            var output = Path.Combine(outputRoot, Path.ChangeExtension(relativePath, ".json"));
            if (translations.Count == 0 || translations.All(line => line.Trash == true))
            {
                if (File.Exists(output))
                {
                    File.Delete(output);
                }
                continue;
            }
            File.WriteAllText(output, JsonSerializer.Serialize(translations, TextConverter.JsonOptions).Replace(@"\u3000", "\u3000"), UTF8NoBOM);
        }
    }


    public static void ImportText(string inputRoot, string replaceRoot, string outputRoot, string? overrideRoot = null)
    {
        var gameConfig = new GameConfig(GameVersion.Invalid);
        var @override = !string.IsNullOrEmpty(overrideRoot);

        Directory.CreateDirectory(outputRoot);
        foreach (var filePath in Directory.GetFiles(inputRoot, "*.bin"))
        {
            var relativePath = Path.GetRelativePath(inputRoot, filePath);
            var replacePath = Path.Combine(replaceRoot, Path.ChangeExtension(relativePath, ".json"));
            if (!File.Exists(replacePath))
            {
                continue;
            }
            var translations = JsonSerializer.Deserialize<List<TranslationItem>>(
                File.ReadAllText(replacePath), TextConverter.JsonOptions
            ) ?? [];

            if (@override)
            {
                var overridePath = Path.Combine(overrideRoot!, Path.ChangeExtension(relativePath, ".json"));
                if (File.Exists(overridePath))
                {
                    var overrideTranslations = JsonSerializer.Deserialize<List<TranslationItem>>(
                        File.ReadAllText(overridePath), TextConverter.JsonOptions
                    ) ?? [];
                    foreach (var line in overrideTranslations)
                    {
                        var existing = translations.FirstOrDefault(t => t.Index == line.Index);
                        if ((existing is not null) && (existing.Trash != true))
                        {
                            existing.Translation = line.Translation;
                        }
                    }
                }
            }

            var textFile = new TextFile(gameConfig, File.ReadAllBytes(filePath), false);
            var lines = textFile.LineData.Select(TextConverter.GetLineString).ToArray();
            foreach (var line in translations)
            {
                if (line.Trash == true)
                {
                    continue;
                }
                lines[line.Index] = line.Translation ?? string.Empty;
            }
            textFile.LineData = TextConverter.ConvertLinesToData(lines);

            var output = Path.Combine(outputRoot, relativePath);
            File.WriteAllBytes(output, textFile.Data);
        }
    }
}
