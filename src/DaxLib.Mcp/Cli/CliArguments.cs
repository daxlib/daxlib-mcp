using System.Collections.Generic;

namespace DaxLib.Mcp.Cli;

internal sealed record CliArguments(CliAction Action)
{
    /// <summary>
    /// Parses the command line. Unknown arguments are ignored.
    /// </summary>
    public static CliArguments Parse(IReadOnlyList<string> args)
    {
        var start = false;
        var showVersion = false;
        var showUsage = false;

        foreach (var arg in args)
        {
            switch (arg.ToUpperInvariant())
            {
                case "--START":
                    start = true;
                    break;
                case "--VERSION":
                    showVersion = true;
                    break;
                case "--HELP" or "-H":
                    showUsage = true;
                    break;
            }
        }

        var action =
            showVersion ? CliAction.ShowVersion :
            showUsage ? CliAction.ShowUsage :
            start ? CliAction.Start :
            CliAction.ShowInformation;

        return new CliArguments(action);
    }
}
