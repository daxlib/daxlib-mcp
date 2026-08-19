using System;

namespace DaxLib.Mcp.Cli;

/// <summary>
/// One line of console output, and the colour it is written in.
/// </summary>
internal readonly record struct CliConsoleLine(string Text, ConsoleColor? Color = null);
