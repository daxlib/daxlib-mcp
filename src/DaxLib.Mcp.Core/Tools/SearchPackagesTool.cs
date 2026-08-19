using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.Rendering;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DaxLib.Mcp.Core.Tools;

[McpServerToolType]
internal sealed class SearchPackagesTool(IGalleryClient galleryClient, ToolCallInvocation toolCallInvocation)
{
    private const int MinimumLimit = 1;
    private const int MaximumLimit = 100;

    private readonly IGalleryClient _galleryClient = galleryClient;
    private readonly ToolCallInvocation _toolCallInvocation = toolCallInvocation;

    [McpServerTool(
        Name = ToolNames.SearchPackages,
        Title = "Search DAX Lib packages",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = true)]
    [Description(
        $"""
        Search DAX Lib (daxlib.org), the public gallery of user-defined DAX function libraries.
        Returns a paginated list of matching packages with their id, latest version, total downloads, description, and README availability.
        The user must choose which matching package to use; never select one automatically.
        """)]
    public Task<CallToolResult> SearchPackagesAsync(
        [Description("A short search keyword, e.g. `svg`. Prefix with `packageid:` for an exact package id lookup. An empty string lists all packages.")]
        string query = "",
        [Description("Maximum number of packages to return per page. Range: 1–100. Default: 25.")]
        int limit = 25,
        [Description("Number of packages to skip for paging. Default: 0.")]
        int skip = 0,
        [Description("Whether to include packages whose latest version is a prerelease. Default: true.")]
        bool includePrerelease = true,
        CancellationToken cancellationToken = default)
    {
        return _toolCallInvocation.RunAsync(ToolNames.SearchPackages, async () =>
        {
            var effectiveQuery = query?.Trim() ?? string.Empty;
            var effectiveLimit = Math.Clamp(limit, MinimumLimit, MaximumLimit);
            var effectiveSkip = Math.Max(skip, 0);

            var request = new SearchRequest(effectiveQuery, effectiveSkip, effectiveLimit, includePrerelease);

            var packages = await _galleryClient.SearchPackagesAsync(request, cancellationToken);

            if (packages.Count == 0)
                return SearchResultRenderer.NoResults(request);

            return SearchResultRenderer.Render(packages, request);
        });
    }
}
