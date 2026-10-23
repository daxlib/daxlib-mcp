using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Client;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.QueryDefinitions;
using DaxLib.Mcp.Core.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.QueryDefinitions;

/// <summary>
/// Calls <c>get_function_definitions</c> on every package of the real DAX Lib gallery and, when a
/// Power BI semantic model is configured, runs each DEFINE block on the Power BI service. They need
/// the network, so they run only when explicitly requested and never in CI:
/// <list type="bullet">
/// <item><c>DAXLIB_MCP_LIVE_GALLERY_TESTS=1</c> enables the tests.</item>
/// <item>
/// For the engine test, also <c>DAXLIB_MCP_PBI_WORKSPACE_ID</c>, <c>DAXLIB_MCP_PBI_DATASET_ID</c>,
/// and <c>DAXLIB_MCP_PBI_ACCESS_TOKEN</c> (for example from
/// <c>az account get-access-token --resource https://analysis.windows.net/powerbi/api</c>). The
/// tenant setting for the executeQueries API must be on. Never commit these values.
/// </item>
/// </list>
/// </summary>
[Trait("Category", "LiveGallery")]
public sealed class GetFunctionDefinitionsLiveTests
{
    private const string OptInVariable = "DAXLIB_MCP_LIVE_GALLERY_TESTS";
    private const string WorkspaceVariable = "DAXLIB_MCP_PBI_WORKSPACE_ID";
    private const string DatasetVariable = "DAXLIB_MCP_PBI_DATASET_ID";
    private const string TokenVariable = "DAXLIB_MCP_PBI_ACCESS_TOKEN";

    /// <summary>
    /// The packages whose latest version has a function name part that the current DAX parser
    /// rejects (<c>DaxLib.SVG.Element.Group</c>, <c>Ankush.SemanticInfo.Model.Describe</c>). The
    /// owners publish renamed versions; remove a package from the list when its latest version runs.
    /// </summary>
    private static readonly string[] s_knownEngineFailures = ["Ankush.SemanticInfo", "DaxLib.SVG"];

    [Fact]
    public async Task Gallery_EveryLatestPackage_ReturnsADefineBlock()
    {
        RequireOptIn();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = CreateClient();
        var (galleryClient, tool) = CreateTool(client);

        foreach (var package in await ListPackagesAsync(galleryClient, cancellationToken))
        {
            var (result, block) = await GetDefinitionsAsync(tool, package.Id.ToNormalizedString(), cancellationToken);

            Assert.True(result.IsError != true, $"{package.Id}: {Text(result)}");
            Assert.StartsWith("DEFINE\n", block, StringComparison.Ordinal);
            Assert.DoesNotContain("FUNCTION '", block, StringComparison.Ordinal);
            Assert.DoesNotContain("```", block, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task PowerBIService_EveryLatestPackage_RunsExceptTheKnownFailures()
    {
        RequireOptIn();
        var workspaceId = Environment.GetEnvironmentVariable(WorkspaceVariable);
        var datasetId = Environment.GetEnvironmentVariable(DatasetVariable);
        var token = Environment.GetEnvironmentVariable(TokenVariable);
        Assert.SkipWhen(
            string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(datasetId) || string.IsNullOrEmpty(token),
            $"Set {WorkspaceVariable}, {DatasetVariable} and {TokenVariable} to run the DEFINE blocks on the Power BI service.");

        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = CreateClient();
        var (galleryClient, tool) = CreateTool(client);
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var endpoint = new Uri($"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/executeQueries");

        List<string> failures = [];

        foreach (var package in await ListPackagesAsync(galleryClient, cancellationToken))
        {
            var id = package.Id.ToNormalizedString();
            var (_, block) = await GetDefinitionsAsync(tool, id, cancellationToken);

            using var response = await http.PostAsJsonAsync(
                endpoint,
                new { queries = new[] { new { query = $"{block}\nEVALUATE {{ 1 }}" } }, serializerSettings = new { includeNulls = true } },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                failures.Add(id);
        }

        Assert.Equal(s_knownEngineFailures, failures.Order(StringComparer.Ordinal));
    }

    private static DaxLibClient CreateClient() => new(new ProductInformation("daxlib-mcp-tests", "0.0.0"));

    private static (IGalleryClient Gallery, GetFunctionDefinitionsTool Tool) CreateTool(DaxLibClient client)
    {
        var galleryClient = new GalleryClient(client, NullLogger<GalleryClient>.Instance);
        var packageProvider = new PackageProvider(galleryClient, NullLogger<PackageProvider>.Instance);

        var tool = new GetFunctionDefinitionsTool(
            new PackageResolver(galleryClient, NullLogger<PackageResolver>.Instance),
            packageProvider,
            new DependencyResolver(galleryClient, packageProvider),
            new ToolCallInvocation(NullLogger<ToolCallInvocation>.Instance),
            NullLogger<GetFunctionDefinitionsTool>.Instance);

        return (galleryClient, tool);
    }

    private static async Task<List<GalleryPackage>> ListPackagesAsync(IGalleryClient galleryClient, CancellationToken cancellationToken)
    {
        const int PageSize = 100;
        List<GalleryPackage> packages = [];

        while (true)
        {
            var page = await galleryClient.SearchPackagesAsync(
                new SearchRequest(string.Empty, packages.Count, PageSize, IncludePrerelease: true), cancellationToken);
            packages.AddRange(page);

            if (page.Count < PageSize)
                return packages;
        }
    }

    private static async Task<(CallToolResult Result, string Block)> GetDefinitionsAsync(
        GetFunctionDefinitionsTool tool, string id, CancellationToken cancellationToken)
    {
        var result = await tool.GetFunctionDefinitionsAsync(id, cancellationToken: cancellationToken);
        var block = result.Content.OfType<EmbeddedResourceBlock>().Select(content => content.Resource).OfType<TextResourceContents>().SingleOrDefault()?.Text;

        return (result, block ?? string.Empty);
    }

    private static string Text(CallToolResult result) =>
        string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    private static void RequireOptIn() =>
        Assert.SkipWhen(
            Environment.GetEnvironmentVariable(OptInVariable) is not "1",
            $"Set {OptInVariable}=1 to run the tests against the live DAX Lib gallery.");
}
