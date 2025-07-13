using Mono.Options;

namespace PCTRGen6Tools.Commands;

public class ExportCommand : Command
{
    private string? inputRoot;
    private string? outputRoot;

    public ExportCommand() : base("export", "Export from garc files")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the files", v => inputRoot = v},
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

        Helper.Export(inputRoot, outputRoot);
        return 0;
    }
}
