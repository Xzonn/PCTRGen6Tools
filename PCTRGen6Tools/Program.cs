using Mono.Options;
using PCTRGen6Tools.Commands;

namespace PCTRGen6Tools;

internal partial class Program
{
    static int Main(string[] args)
    {
        var game = Environment.GetEnvironmentVariable("XZ_GEN6_GAME_VERSION")??"";
        if (game == "XY")
        {
            Environment.CurrentDirectory = @"C:\Codes\PCTRGen6ToolsBuild";
            var inputRoot = @"original_files";
            var replaceInputRoot = @"temp\import";
            var replaceOutputRoot = @"temp\output";

            Helper.ImportText(
                @"unpacked\japanese\XY\a072.garc",
                @"texts\XY\zh_Hans\gametext",
                @"temp\import\japanese\XY\a072.garc"
            );
            Helper.ImportText(
                @"unpacked\japanese\XY\a080.garc",
                @"texts\XY\zh_Hans\stroytext",
                @"temp\import\japanese\XY\a080.garc"
            );

            Helper.Import(inputRoot, replaceInputRoot, replaceOutputRoot);
            File.Copy(
                @"temp\output\japanese\XY\a185.garc",
                @"C:\Users\Xzonn\AppData\Roaming\Citra\load\mods\0004000000055E00\romfs\a\1\8\5",
                true
            );
            File.Copy(
                @"temp\output\japanese\XY\a072.garc",
                @"C:\Users\Xzonn\AppData\Roaming\Citra\load\mods\0004000000055E00\romfs\a\0\7\2",
                true
            );
            File.Copy(
                @"temp\output\japanese\XY\a080.garc",
                @"C:\Users\Xzonn\AppData\Roaming\Citra\load\mods\0004000000055E00\romfs\a\0\8\0",
                true
            );
        }
        else
        {
            CommandSet commands = new("PCTRGen6Tools")
            {
                new ExportCommand(),
                new ImportCommand(),
                new ExportTextCommand(),
                new ImportTextCommand(),
            };

            return commands.Run(args);
        }

        return 0;
    }

    static void Main_3(string[] args)
    {
        var inputRoot = @"original_files";
        var outputRoot = @"unpacked";
        var replaceInputRoot = @"temp\import";
        var replaceOutputRoot = @"temp\output";

        Helper.Export(inputRoot, outputRoot);
        Helper.Import(inputRoot, replaceInputRoot, replaceOutputRoot);
    }

    static void Main_2(string[] args)
    {

        Helper.ExportText(
            @"unpacked\japanese\ORAS\a071.garc",
            @"texts\ORAS\ja\gametext",
            @"unpacked\japanese\ORAS\a071.garc",
            "ORAS_Game_"
        );
        Helper.ExportText(
            @"unpacked\japanese\ORAS\a072.garc",
            @"texts\ORAS\ja_KS\gametext",
            @"unpacked\japanese\ORAS\a071.garc",
            "ORAS_Game_"
        );
        Helper.ExportText(
            @"unpacked\chinese\ORAS\a071.garc",
            @"texts\ORAS\zh_Hans\gametext",
            @"unpacked\japanese\ORAS\a071.garc",
            "ORAS_Game_"
        );
        Helper.ExportText(
            @"unpacked\japanese\ORAS\a079.garc",
            @"texts\ORAS\ja\stroytext",
            @"unpacked\japanese\ORAS\a079.garc",
            "ORAS_Story_"
        );
        Helper.ExportText(
            @"unpacked\japanese\ORAS\a080.garc",
            @"texts\ORAS\ja_KS\stroytext",
            @"unpacked\japanese\ORAS\a079.garc",
            "ORAS_Story_"
        );
        Helper.ExportText(
            @"unpacked\chinese\ORAS\a079.garc",
            @"texts\ORAS\zh_Hans\stroytext",
            @"unpacked\japanese\ORAS\a079.garc",
            "ORAS_Story_"
        );

    }

    private static void Main_1(string[] args)
    {
        Helper.ExportText(
            @"unpacked\japanese\XY\a072.garc",
            @"texts\XY\ja\gametext",
            @"unpacked\japanese\XY\a072.garc",
            "XY_Game_"
        );
        Helper.ExportText(
            @"unpacked\japanese\XY\a073.garc",
            @"texts\XY\ja_KS\gametext",
            @"unpacked\japanese\XY\a072.garc",
            "XY_Game_"
        );
        Helper.ExportText(
            @"unpacked\chinese\XY\a072.garc",
            @"texts\XY\zh_Hans\gametext",
            @"unpacked\japanese\XY\a072.garc",
            "XY_Game_"
        );
        Helper.ExportText(
            @"unpacked\japanese\XY\a080.garc",
            @"texts\XY\ja\stroytext",
            @"unpacked\japanese\XY\a080.garc",
            "XY_Story_"
        );
        Helper.ExportText(
            @"unpacked\japanese\XY\a081.garc",
            @"texts\XY\ja_KS\stroytext",
            @"unpacked\japanese\XY\a080.garc",
            "XY_Story_"
        );
        Helper.ExportText(
            @"unpacked\chinese\XY\a080.garc",
            @"texts\XY\zh_Hans\stroytext",
            @"unpacked\japanese\XY\a080.garc",
            "XY_Story_"
        );

        File.Copy(
            @"temp\output\japanese\XY\a185.garc",
            @"C:\Users\Xzonn\AppData\Roaming\Citra\load\mods\0004000000055E00\romfs\a\1\8\5",
            true
        );
    }
}
