# Troubleshooting DAX Lib MCP

DAX Lib MCP runs locally and writes diagnostic information to the configured MCP client. Diagnostic
information remains on the local computer unless it is explicitly shared.

## MCP server logging

Most problems can be diagnosed from the server output already available in the MCP client. Look for
the DAX Lib MCP server in the client's MCP settings and open its output or logs.

In Visual Studio Code:

1. Open the Command Palette.
2. Run `MCP: List Servers`.
3. Select DAX Lib MCP.
4. Choose `Show Output`.

After opening the output, repeat the failing action and review the most recent messages. Copy the
messages related to the failure for further investigation.

## Collect a detailed diagnostic trace

DAX Lib MCP also uses .NET EventSource to emit detailed diagnostic information. These events can be
captured with `dotnet-trace`.

See the [official dotnet-trace documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
for usage and installation options.

The standalone executable does not require the .NET SDK. If the .NET SDK is already installed, the
tool can alternatively be installed with:

```powershell
dotnet tool install --global dotnet-trace
```

To capture the MCP logs:

1. Keep the MCP client and DAX Lib MCP running.
2. Run `.\dotnet-trace.exe ps` and find the process ID for `daxlib-mcp.exe`.
3. Run `.\dotnet-trace.exe collect -p <PID> --providers Microsoft-Extensions-Logging:4:5 -o daxlib-mcp.nettrace`.
4. Reproduce the problem, then press `Enter` to stop collection.

The resulting `daxlib-mcp.nettrace` file can be opened with Visual Studio or PerfView. When the
global tool is used, replace `.\dotnet-trace.exe` with `dotnet-trace` in the commands above.


