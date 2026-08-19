using System;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.Composition;

/// <summary>
/// Freezes the published contract of the MCP server: the tool names and their input schemas.
/// </summary>
/// <remarks>
/// This class is intentionally limited to these two contracts, the ones a rename or a signature
/// change would break silently. Do not reintroduce tests for result envelopes, messages, caching
/// hints, capability declarations, resource behavior, or input clamping: they pin wording and
/// transport details that are free to change during development.
/// </remarks>
public sealed class SurfaceTests
{
    [Fact]
    public async Task ListTools_AnyClient_ReturnsTheFrozenToolNames()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await McpServerHarness.StartAsync(cancellationToken);

        var tools = await harness.Client.ListToolsAsync(cancellationToken: cancellationToken);

        Assert.Equal(
            ["get_package", "get_package_readme", "install_package", "list_local_instances", "search_packages"],
            [.. tools.Select(tool => tool.Name).Order(StringComparer.Ordinal)]);
    }

    [Fact]
    public async Task ListTools_AnyClient_ReturnsTheFrozenInputSchemas()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await McpServerHarness.StartAsync(cancellationToken);

        var tools = await harness.Client.ListToolsAsync(cancellationToken: cancellationToken);

        AssertSchema(tools.Single(tool => tool.Name == "search_packages"),
            expectedProperties: ["query", "limit", "skip", "includePrerelease"],
            expectedRequired: []);
        AssertSchema(tools.Single(tool => tool.Name == "get_package"),
            expectedProperties: ["id", "version"],
            expectedRequired: ["id"]);
        AssertSchema(tools.Single(tool => tool.Name == "get_package_readme"),
            expectedProperties: ["id", "version"],
            expectedRequired: ["id"]);
        AssertSchema(tools.Single(tool => tool.Name == "install_package"),
            expectedProperties: ["id", "version", "dataSource"],
            expectedRequired: ["id", "dataSource"]);
        AssertSchema(tools.Single(tool => tool.Name == "list_local_instances"),
            expectedProperties: [],
            expectedRequired: []);

        static void AssertSchema(McpClientTool tool, string[] expectedProperties, string[] expectedRequired)
        {
            var schema = tool.JsonSchema;
            string[] properties = [.. schema.GetProperty("properties").EnumerateObject().Select(property => property.Name)];
            string[] required = schema.TryGetProperty("required", out var requiredElement)
                ? [.. requiredElement.EnumerateArray().Select(element => element.GetString()!)]
                : [];

            Assert.Equal(expectedProperties.Order(StringComparer.Ordinal), properties.Order(StringComparer.Ordinal));
            Assert.Equal(expectedRequired.Order(StringComparer.Ordinal), required.Order(StringComparer.Ordinal));
        }
    }
}
