using Mono.Options;
using PCTRGen6Tools.Commands;

namespace PCTRGen6Tools;

internal partial class Program
{
    static int Main(string[] args)
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
}
