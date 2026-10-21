using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using DaxLib.Mcp.Core;

namespace DaxLib.Mcp.Cli;

/// <summary>
/// The text this executable writes to the console: the command line reference, a description of
/// the server, and the instructions to register it with an MCP client.
/// </summary>
internal static class CliConsoleMessages
{
    private const string ServerName = "daxlib-mcp";
    private const string NpmPackage = "@daxlib/mcp";
    private const string RepositoryUrl = "https://github.com/daxlib/daxlib-mcp";
    private const string Description = "Local MCP server for DAX Lib (daxlib.org), a centralized open repository of reusable DAX function libraries (packages).";
    private const string Wordmark = """
         ____      _    __  __   _      _   _       __  __   ____  ____
        |  _ \    / \   \ \/ /  | |    (_) | |__   |  \/  | / ___||  _ \
        | | | |  / _ \   \  /   | |    | | | '_ \  | |\/| || |    | |_) |
        | |_| | / ___ \  /  \   | |___ | | | |_) | | |  | || |___ |  __/
        |____/ /_/   \_\/_/\_\  |_____||_| |_.__/  |_|  |_| \____||_|
        """;

    private static readonly string[] s_npmArguments = ["-y", NpmPackage, "--start"];
    private static readonly string[] s_executableArguments = ["--start"];

    public static void WriteUsage() => Write(UsageLines());
    public static void WriteInformation() => Write(InformationLines());

    /// <summary>
    /// Holds the window open until a key is pressed, so that the output remains visible when the
    /// executable is started with a double click. Does nothing when stdin is redirected: no keyboard
    /// is available, and a scripted run must print and exit like any other command.
    /// </summary>
    public static void WaitForKey()
    {
        if (Console.IsInputRedirected)
            return;

        Write([new(string.Empty), new("Press any key to close...")]);

        Console.ReadKey(intercept: true);
    }

    private static IReadOnlyList<CliConsoleLine> UsageLines() =>
    [
        new("DAX Lib MCP server", ConsoleColor.Yellow),
        new(string.Empty),
        new("Usage:", ConsoleColor.Cyan),
        new($"  {ServerName} [--start | --version | --help]"),
        new(string.Empty),
        new("Options:", ConsoleColor.Cyan),
        .. Block("""
              --start        Start the MCP server.
              --version      Show the version.
              --help, -h     Show this help.
            """),
    ];

    private static IReadOnlyList<CliConsoleLine> InformationLines() =>
    [
        .. Block(Wordmark, ConsoleColor.Yellow),
        new(string.Empty),
        new($"Version: {McpServerInfo.Version}"),
        new(string.Empty),
        .. Block(Description),
        new(string.Empty),
        new("MCP configuration (for manual registration):", ConsoleColor.Cyan),
        new(string.Empty),
        .. Block(McpRegistrationSnippet("npx", s_npmArguments), ConsoleColor.Yellow),
        .. McpExecutableRegistrationLines(),
        new(string.Empty),
        new("Visual Studio Code installation (CTRL + Click to open):", ConsoleColor.Cyan),
        new(string.Empty),
        new($"  {VisualStudioCodeInstallLink()}", ConsoleColor.Blue),
        new(string.Empty),
        new("More information:", ConsoleColor.Cyan),
        new(string.Empty),
        new($"  {RepositoryUrl}", ConsoleColor.Blue),
        new(string.Empty),
        new($"Run '{ServerName} --help' for the command line options."),
    ];

    /// <summary>
    /// The registration snippet for running the executable directly (the zipped GitHub release)
    /// instead of the NPM package.
    /// </summary>
    private static IEnumerable<CliConsoleLine> McpExecutableRegistrationLines()
    {
        if (Environment.ProcessPath is not { Length: > 0 } processPath)
            return [];

        return
        [
            new CliConsoleLine(string.Empty),
            new CliConsoleLine("Or register this executable directly:", ConsoleColor.Cyan),
            new CliConsoleLine(string.Empty),
            .. Block(McpRegistrationSnippet(processPath, s_executableArguments), ConsoleColor.Yellow),
        ];
    }

    private static string McpRegistrationSnippet(string command, IReadOnlyList<string> arguments)
    {
        var commandValue = JsonString(command);
        var argumentList = string.Join(", ", arguments.Select(JsonString));

        return $$"""
              {
                "mcpServers": {
                  "{{ServerName}}": {
                    "command": {{commandValue}},
                    "args": [ {{argumentList}} ]
                  }
                }
              }
            """;

        static string JsonString(string value) => $"\"{JsonEncodedText.Encode(value)}\"";
    }

    private static string VisualStudioCodeInstallLink()
    {
        var arguments = new JsonArray();

        foreach (var argument in s_npmArguments)
            arguments.Add((JsonNode)argument);

        var configuration = new JsonObject
        {
            ["command"] = "npx",
            ["args"] = arguments,
        };

        return $"https://vscode.dev/redirect/mcp/install?name={ServerName}&config={Uri.EscapeDataString(configuration.ToJsonString())}";
    }

    /// <summary>Splits a multi-line literal into lines that all take the same colour.</summary>
    private static IEnumerable<CliConsoleLine> Block(string text, ConsoleColor? color = null) =>
        text.ReplaceLineEndings("\n").Split('\n').Select(line => new CliConsoleLine(line, color));

    private static void Write(IReadOnlyList<CliConsoleLine> lines)
    {
        // Redirected output has no terminal to colour. NO_COLOR is the convention for disabling colours.
        var colored = !Console.IsOutputRedirected
            && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

        foreach (var line in lines)
        {
            if (colored && line.Color is { } color)
            {
                Console.ForegroundColor = color;
                Console.Out.WriteLine(line.Text);
                Console.ResetColor();
            }
            else
            {
                Console.Out.WriteLine(line.Text);
            }
        }
    }
}
