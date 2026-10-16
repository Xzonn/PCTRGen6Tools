using Mono.Options;

namespace PCTRGen6Tools.Commands;

public class ImportBannerCommand : Command
{
    private string? inputRoot;
    private string? replaceRoot;
    private string? outputRoot;

    public ImportBannerCommand() : base("import-banner", "Import PNG images into banner slots")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the BCMDL files", v => inputRoot = v},
            {"r|replace=", "The path to the PNG images", v => replaceRoot = v},
            {"o|output=", "The output path", v => outputRoot = v},
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(inputRoot) || string.IsNullOrWhiteSpace(replaceRoot) ||
            string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new ArgumentException("Missing required arguments");
        }

        Helper.ImportBanner(inputRoot, replaceRoot, outputRoot);
        return 0;
    }
}
