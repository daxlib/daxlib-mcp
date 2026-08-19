using System;
using System.Globalization;

namespace DaxLib.Mcp.Core.Infrastructure.AnalysisServices;

/// <summary>
/// Represents an instance of the Analysis Services engine running on the local machine.
/// </summary>
internal sealed record AnalysisServicesInstance(
    int ProcessId,
    int Port,
    DateTime StartTime,
    string? ParentProcessName,
    string? ParentProcessWindowTitle)
{
    private const string PBIDesktopProcessName = "PBIDesktop";

    /// <summary>
    /// The connection data source, in the form <c>localhost:{Port}</c>.
    /// </summary>
    public string DataSource
        => $"localhost:{Port.ToString(CultureInfo.InvariantCulture)}";

    public bool IsPowerBIDesktop
        => string.Equals(ParentProcessName, PBIDesktopProcessName, StringComparison.OrdinalIgnoreCase);
}
