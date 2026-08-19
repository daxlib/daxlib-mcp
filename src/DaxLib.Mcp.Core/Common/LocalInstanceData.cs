using System;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// Data returned by the <see cref="DaxLib.Mcp.Core.Tools.ListLocalInstancesTool"/> tool.
/// The property names are the wire contract.
/// </summary>
internal sealed record LocalInstanceData(
    int ProcessId,
    int Port,
    DateTime StartTime,
    string? ParentProcessWindowTitle,
    string DataSource);
