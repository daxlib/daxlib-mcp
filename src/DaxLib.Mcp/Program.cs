using System;
using DaxLib.Mcp.Cli;
using DaxLib.Mcp.Core;
using DaxLib.Mcp.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var arguments = CliArguments.Parse(args);

switch (arguments.Action)
{
    case CliAction.ShowVersion:
        Console.WriteLine(McpServerInfo.Version);
        return 0;

    case CliAction.ShowUsage:
        CliConsoleMessages.WriteUsage();
        CliConsoleMessages.WaitForKey();
        return 0;

    case CliAction.ShowInformation:
        CliConsoleMessages.WriteInformation();
        CliConsoleMessages.WaitForKey();
        return 0;
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    // This process is a stdio child launched per session, not a configurable application
    // host. It has nothing to configure, so none of the host defaults are used.
    DisableDefaults = true,
    // A stdio child process inherits the client's working directory; anchor to the executable.
    ContentRootPath = AppContext.BaseDirectory,
});

// stdout carries the MCP JSON-RPC stream; every log line must go to stderr.
builder.Logging.AddConsole(static options => options.LogToStandardErrorThreshold = LogLevel.Trace);
// Keep diagnostics local and opt-in: dotnet-trace enables this provider only while collecting.
builder.Logging.AddEventSourceLogger();

builder.Services
    .AddDaxLibMcpServer()
    .WithStdioServerTransport();

await builder.Build().RunAsync();
return 0;
