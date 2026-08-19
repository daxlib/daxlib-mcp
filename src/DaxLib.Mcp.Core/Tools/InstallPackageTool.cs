using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.AnalysisServices;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.Rendering;
using DaxLib.Mcp.Core.SdkModeling;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DaxLib.Mcp.Core.Tools;

[McpServerToolType]
internal sealed partial class InstallPackageTool(
    PackageResolver packageResolver,
    IPackageProvider packageProvider,
    IPackageInstaller installer,
    IAnalysisServicesInstanceLocator instanceLocator,
    ToolCallInvocation toolCallInvocation,
    ILogger<InstallPackageTool> logger)
{
    private readonly PackageResolver _packageResolver = packageResolver;
    private readonly IPackageProvider _packageProvider = packageProvider;
    private readonly IPackageInstaller _installer = installer;
    private readonly IAnalysisServicesInstanceLocator _instanceLocator = instanceLocator;
    private readonly ToolCallInvocation _toolCallInvocation = toolCallInvocation;
    private readonly ILogger<InstallPackageTool> _logger = logger;

    [McpServerTool(
        Name = ToolNames.InstallPackage,
        Title = "Install package into Power BI Desktop",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = true)]
    [Description(
        $"""
        Install a DAX Lib package into a Microsoft tabular semantic model open in a local Power BI Desktop instance.
        Adds the package's user-defined DAX functions to the model, replacing any existing function with the same name; no other model object is changed.
        """)]
    public Task<CallToolResult> InstallPackageAsync(
        [Description("Exact package id, e.g. 'DaxLib.SVG'. Case-insensitive.")]
        string id,
        [Description(
            $"Target instance in `host:port` format, e.g. `localhost:51542`, exactly as returned by `{ToolNames.ListLocalInstances}`. " +
            "Must be explicitly chosen by the user.")]
        string dataSource,
        [Description("Exact package version, e.g. '2.0.0'. If omitted, the latest published version is used, prerelease included.")]
        string? version = null,
        McpServer? server = null,
        CancellationToken cancellationToken = default)
    {
        return _toolCallInvocation.RunAsync(ToolNames.InstallPackage, async () =>
        {
            // TODO: Consider MCP progress notifications across the download, parse, connect and
            // save steps, for clients that surface them.
            var instance = _instanceLocator.FindInstance(dataSource, cancellationToken);

            // The locator reports every local instance; only Power BI Desktop is a supported target.
            if (instance is null || !instance.IsPowerBIDesktop)
                return InstallMessageRenderer.UnknownTarget();

            var resolution = await _packageResolver.ResolveAsync(id, version, cancellationToken);

            if (!resolution.IsResolved)
                return PackageLookupRenderer.Render(resolution.Failure);

            var resolvedPackage = resolution.Package;
            var packageId = resolvedPackage.Id.ToNormalizedString();
            var packageVersion = resolvedPackage.Version.ToNormalizedString();

            var confirmation = await InstallConfirmation.RequestAsync(
                server, packageId, packageVersion, instance, cancellationToken);

            if (confirmation is InstallConfirmation.Outcome.Declined)
                return InstallMessageRenderer.Cancelled(packageId, packageVersion);

            using var package = await _packageProvider.GetPackageAsync(
                resolvedPackage.Id,
                resolvedPackage.Version,
                cancellationToken);

            if (package is null)
                return PackageLookupRenderer.Render(
                    new PackageLookupFailure.UnknownVersion(resolvedPackage.Id, packageVersion, resolution.Versions));

            // Built from the discovered instance, never from the caller's dataSource value.
            var connectionInfo = new ConnectionInfo(instance.DataSource, McpServerInfo.Name);
            var outcome = _installer.Install(package, connectionInfo);
            LogPackageInstallOutcome(outcome);

            return InstallMessageRenderer.Render(outcome, packageId, packageVersion, instance.DataSource);
        });
    }
}
