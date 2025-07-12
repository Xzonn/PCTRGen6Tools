using Mono.Options;

namespace PCTRGen6.Commands;

public class ImportCommand : Command
{
    private string? inputRoot;
    private string? replaceRoot;
    private string? outputRoot;

    public ImportCommand() : base("import", "Import into garc files")
    {
        Options = new OptionSet
        {
            {"p|path=", "The path to the original files", v => inputRoot = v},
            {"r|replace=", "The path to the files to replace", v => replaceRoot = v},
            {"o|output=", "The output path", v => outputRoot = v},
        };
    }

    public override int Invoke(IEnumerable<string> arguments)
    {
        Options.Parse(arguments);

        if (string.IsNullOrWhiteSpace(inputRoot) || string.IsNullOrWhiteSpace(replaceRoot) || string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new ArgumentException("Missing required arguments");
        }

        Helper.Import(inputRoot, replaceRoot, outputRoot);
        return 0;
    }
}