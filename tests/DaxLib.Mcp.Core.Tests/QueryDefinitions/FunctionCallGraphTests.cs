using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DaxLib.Mcp.Core.QueryDefinitions;
using DaxLib.Mcp.Core.Tests.Fixtures;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.QueryDefinitions;

/// <summary>
/// Compares the calls between functions of the same package with the golden data of the
/// investigation (<c>Expected/call-graph-tokenizer.json</c>), which a tokenizer produced on every
/// published package. A regex found 16 more edges, all inside strings or comments.
/// </summary>
public sealed class FunctionCallGraphTests
{
    [Theory]
    [InlineData("DaxLib.SVG", "2.0.0")]
    [InlineData("XU.Calendar", "0.1.0")]
    [InlineData("DaxLib.Filtering", "0.1.2")]
    [InlineData("DaxLib.FormatString", "0.1.2")]
    [InlineData("DaxPatterns.AbcClassification", "0.1.4")]
    [InlineData("DaxLib.Sample", "0.1.8")]
    [InlineData("DaxLib.Records", "0.9.0-beta")]
    public void LocalCalls_PublishedPackage_EqualTheGoldenCallGraph(string packageId, string version)
    {
        var catalog = Catalog(packageId, version);
        var expected = Golden()[packageId];

        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), catalog.Functions.Select(function => function.Name).Order(StringComparer.Ordinal));

        foreach (var function in catalog.Functions)
            Assert.Equal(expected[function.Name].Order(StringComparer.Ordinal), LocalCalls(catalog, function));
    }

    [Fact]
    public void LocalCalls_Svg_HasTwentyFourCallers()
    {
        var catalog = Catalog("DaxLib.SVG", "2.0.0");

        Assert.Equal(24, catalog.Functions.Count(function => LocalCalls(catalog, function).Any()));
    }

    /// <summary>
    /// XU.Calendar.About lists the other functions of the package inside a string: a regex reads
    /// three calls there, the tokenizer none.
    /// </summary>
    [Fact]
    public void LocalCalls_NamesInsideAString_AreNotCalls()
    {
        var catalog = Catalog("XU.Calendar", "0.1.0");
        Assert.True(catalog.TryGetFunction("XU.Calendar.About", out var about));

        Assert.Contains("XU.Calendar.Build", about.Expression, StringComparison.Ordinal);
        Assert.Empty(LocalCalls(catalog, about));
    }

    [Fact]
    public void BuiltInFunctions_DottedNames_AreKnown()
    {
        Assert.True(DaxBuiltInFunctions.Contains("NORM.S.DIST"));
        Assert.True(DaxBuiltInFunctions.Contains("percentile.inc"));
        Assert.True(DaxBuiltInFunctions.Contains("CALCULATE"));
        Assert.False(DaxBuiltInFunctions.Contains("DaxLib.SVG.Viz.Pill"));
    }

    private static IEnumerable<string> LocalCalls(FunctionCatalog catalog, CatalogFunction function) =>
        function.CallNames
            .Where(name => !DaxBuiltInFunctions.Contains(name))
            .Select(name => catalog.TryGetFunction(name, out var callee) ? callee.Name : null)
            .OfType<string>()
            .Order(StringComparer.Ordinal);

    private static FunctionCatalog Catalog(string packageId, string version)
    {
        using var package = FixtureLoader.OpenPackage(packageId, version);

        return FunctionCatalog.Create(package);
    }

    private static Dictionary<string, Dictionary<string, string[]>> Golden() =>
        JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string[]>>>(
            FixtureLoader.ReadText("Expected.call-graph-tokenizer.json"))!;
}
