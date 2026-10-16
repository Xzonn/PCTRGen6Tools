using Mono.Options;

namespace PCTRGen6Tools.Commands;

public class ExportImagesCommand : Command
{
    private string? inputRoot;
    private string? outputRoot;

    public ExportImagesCommand() : base("export-images", "Export PNG images from files")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the original files", v => inputRoot = v},
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

        Helper.ExportImages(inputRoot, outputRoot);
        return 0;
    }
}
