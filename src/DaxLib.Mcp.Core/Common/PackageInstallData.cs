namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// Data returned by a successful <c>install_package</c>. The installed DAX is not returned.
/// </summary>
internal sealed record PackageInstallData(string DatabaseName, int FunctionCount);
