using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.Rendering;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DaxLib.Mcp.Core.Tools;

[McpServerToolType]
internal sealed class GetPackageTool(PackageResolver packageResolver, ToolCallInvocation toolCallInvocation)
{
    private readonly PackageResolver _packageResolver = packageResolver;
    private readonly ToolCallInvocation _toolCallInvocation = toolCallInvocation;

    [McpServerTool(
        Name = ToolNames.GetPackage,
        Title = "Get package details",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = true)]
    [Description(
        $"""
        Get details for a DAX Lib package.
        Returns its id, version, description, authors, owners, tags, publication date, version-specific download count, README availability, gallery, project and repository links, release notes, and the full list of published versions (newest first).
        Does not return DAX code.
        """)]
    public Task<CallToolResult> GetPackageAsync(
        [Description("Exact package id, e.g. 'DaxLib.SVG'. Case-insensitive.")]
        string id,
        [Description("Exact package version, e.g. '2.0.0'. If omitted, the latest published version is used, prerelease included.")]
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        return _toolCallInvocation.RunAsync(ToolNames.GetPackage, async () =>
        {
            var resolution = await _packageResolver.ResolveAsync(id, version, cancellationToken);

            if (!resolution.IsResolved)
                return PackageLookupRenderer.Render(resolution.Failure);

            return PackageCardRenderer.Render(resolution.Package, resolution.Versions);
        });
    }
}
