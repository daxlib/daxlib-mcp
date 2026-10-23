using System.Collections.Generic;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// Data returned by a successful <c>install_package</c>. The installed DAX is not returned.
/// </summary>
/// <param name="Warnings">Problems that did not stop the install; omitted when there are none.</param>
internal sealed record PackageInstallData(
    string DatabaseName,
    int FunctionCount,
    IReadOnlyList<PackageInstallWarningData>? Warnings = null);

/// <param name="Code"><c>dependencyNotInstalled</c>.</param>
internal sealed record PackageInstallWarningData(string Code, string? Package, string Message);
