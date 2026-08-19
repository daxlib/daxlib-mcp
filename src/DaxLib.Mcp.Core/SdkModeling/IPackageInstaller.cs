using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.SdkModeling;

/// <summary>
/// Installs one published package into one connected model.
/// </summary>
internal interface IPackageInstaller
{
    PackageInstallOutcome Install(Package package, ConnectionInfo connection);
}
