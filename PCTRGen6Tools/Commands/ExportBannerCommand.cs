using Mono.Options;

namespace PCTRGen6Tools.Commands;

public class ExportBannerCommand : Command
{
    private string? inputRoot;
    private string? outputRoot;

    public ExportBannerCommand() : base("export-banner", "Export PNG images from all banner slots")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the BCMDL files", v => inputRoot = v},
            {"o|output=", "The output directory", v => outputRoot = v},
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(inputRoot) || string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new ArgumentException("Missing required arguments");
        }

        Helper.ExportBanner(inputRoot, outputRoot);
        return 0;
    }
}
