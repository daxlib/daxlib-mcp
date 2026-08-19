using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.Rendering;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DaxLib.Mcp.Core.Tools;

[McpServerToolType]
internal sealed class GetPackageReadmeTool(
    PackageResolver packageResolver,
    IGalleryClient galleryClient,
    ToolCallInvocation toolCallInvocation)
{
    private readonly PackageResolver _packageResolver = packageResolver;
    private readonly IGalleryClient _galleryClient = galleryClient;
    private readonly ToolCallInvocation _toolCallInvocation = toolCallInvocation;

    [McpServerTool(
        Name = ToolNames.GetPackageReadme,
        Title = "Get package README",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = true)]
    [Description(
        $"""
        Get the README published with a specific DAX Lib package version as a Markdown resource.
        """)]
    public Task<CallToolResult> GetPackageReadmeAsync(
        [Description("Exact package id, e.g. 'DaxLib.SVG'. Case-insensitive.")]
        string id,
        [Description("Exact package version, e.g. '2.0.0'. If omitted, the latest published version is used, prerelease included.")]
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        return _toolCallInvocation.RunAsync(ToolNames.GetPackageReadme, async () =>
        {
            var resolution = await _packageResolver.ResolveAsync(id, version, cancellationToken);

            if (!resolution.IsResolved)
                return PackageLookupRenderer.Render(resolution.Failure);

            var package = resolution.Package;

            if (!package.HasReadme)
                return ReadmeRenderer.NoReadme(package);

            var markdown = await _galleryClient.GetReadmeAsync(package.Id, package.Version, cancellationToken);

            if (string.IsNullOrWhiteSpace(markdown))
                return ReadmeRenderer.NoReadme(package);

            return ReadmeRenderer.Render(package, markdown);
        });
    }
}
