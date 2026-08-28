using System;
using System.Diagnostics;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.SdkModeling;

namespace DaxLib.Mcp.Core.Rendering;

/// <summary>
/// Renders a <see cref="PackageInstallOutcome"/> into a <see cref="ToolOutcome"/> with a user-facing message and optional data.
/// </summary>
internal static class InstallMessageRenderer
{
    public static ToolOutcome Render(
        PackageInstallOutcome outcome, string packageId, string packageVersion, string dataSource) => outcome switch
        {
            PackageInstallOutcome.Installed installed => Success(packageId, packageVersion, dataSource, installed),
            PackageInstallOutcome.CompatibilityTooLow o => CompatibilityTooLow(o.ActualLevel, o.MinimumLevel),
            PackageInstallOutcome.UnexpectedDatabaseCount o => UnexpectedDatabaseCount(dataSource, o.DatabaseCount),
            PackageInstallOutcome.PackageDeserializationFailed o => PackageDeserializationFailed(packageId, packageVersion, o.Exception),
            PackageInstallOutcome.ConnectionFailed o => ConnectionFailed(dataSource, o.Exception),
            PackageInstallOutcome.SaveChangesFailed o => SaveChangesFailed(o.Exception),

            _ => throw new UnreachableException($"Unhandled {nameof(PackageInstallOutcome)}: {outcome.GetType().Name}.")
        };

    private static ToolOutcome Success(
       string packageId, string packageVersion, string dataSource, PackageInstallOutcome.Installed installed) =>
       ToolOutcome.Ok(
           $"Installed package {packageId} version {packageVersion} into database '{installed.DatabaseName}' on dataSource {dataSource}: {installed.FunctionCount} functions added or replaced.",
           new PackageInstallData(installed.DatabaseName, installed.FunctionCount));

    public static ToolOutcome UnknownTarget() =>
        ToolOutcome.Failed(
            "Install failed: the specified `dataSource` does not match any local Power BI Desktop instance currently running on this machine.");

    private static ToolOutcome ConnectionFailed(string dataSource, Exception exception) =>
        ToolOutcome.Failed(
            $"Install failed: the connection to the instance {dataSource} could not be established. " +
            $"The engine reported {exception.GetType().Name}: {exception.Message}.");

    private static ToolOutcome CompatibilityTooLow(int actualLevel, int minimumLevel) =>
        ToolOutcome.Failed(
            $"Install failed: the model's compatibility level is {actualLevel}, but this package requires at least {minimumLevel}. " +
            "The server does not change the model's compatibility level because it is a model-wide setting that must be changed deliberately by the user. " +
            $"Ask the user to raise the compatibility level to at least {minimumLevel}, then retry the installation.");

    private static ToolOutcome UnexpectedDatabaseCount(string dataSource, int databaseCount) =>
        ToolOutcome.Failed(
            $"Install failed: the instance {dataSource} was expected to host exactly one database, but hosts {databaseCount}. " +
            "Ask the user to open the target model in Power BI Desktop, then retry.");

    private static ToolOutcome PackageDeserializationFailed(string packageId, string packageVersion, Exception exception) =>
        ToolOutcome.Failed(
            $"Install failed: package {packageId} version {packageVersion} could not be read. " +
            $"The parser reported {exception.GetType().Name}: {exception.Message}. " +
            "The package content itself is invalid, so retrying will not help.");

    private static ToolOutcome SaveChangesFailed(Exception exception) =>
        ToolOutcome.Failed(
            "Install failed: the engine could not save the change. " +
            $"The engine reported {exception.GetType().Name}: {exception.Message}. " +
            "Do not retry automatically.");
}
