using Mono.Options;

namespace PCTRGen6Tools.Commands;

public class ExportTextCommand : Command
{
    private string? inputRoot;
    private string? outputRoot;
    private string? originalRoot;
    private string? keyPrefix;

    public ExportTextCommand() : base("export-text", "Export texts from files")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the files", v => inputRoot = v},
            {"o|output=", "The output directory", v => outputRoot = v},
            {"k|key-prefix=", "The prefix to the key", v => keyPrefix = v},
            {"r|original=", "The path to the original files", v => originalRoot = v},
        };
    }


    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(inputRoot) || string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new ArgumentException("Missing required arguments");
        }

        Helper.ExportText(inputRoot, outputRoot, originalRoot, keyPrefix);
        return 0;
    }
}
