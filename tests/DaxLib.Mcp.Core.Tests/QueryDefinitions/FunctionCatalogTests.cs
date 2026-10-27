using System;
using System.Linq;
using DaxLib.Mcp.Core.QueryDefinitions;
using DaxLib.Mcp.Core.Tests.Fixtures;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.QueryDefinitions;

public sealed class FunctionCatalogTests
{
    [Fact]
    public void Create_Sample_ReadsManifestDescriptionsAndParameters()
    {
        using var package = FixtureLoader.OpenPackage("DaxLib.Sample", "0.1.8");

        var catalog = FunctionCatalog.Create(package);

        Assert.Equal("DaxLib.Sample", catalog.PackageId);
        Assert.Equal("0.1.8", catalog.Version.ToNormalizedString());
        var dependency = Assert.Single(catalog.Dependencies);
        Assert.Equal("DaxLib.Records", dependency.PackageId.ToNormalizedString());
        Assert.Equal("0.9.0-beta", dependency.Version.ToNormalizedString());

        Assert.Equal(
            ["DaxLib.Sample.MyFunc1", "DaxLib.Sample.MyFunc2", "DaxLib.Sample.MyFunc3"],
            catalog.Functions.Select(function => function.Name));

        Assert.True(catalog.TryGetFunction("daxlib.sample.myfunc3", out var myFunc3));
        Assert.Equal("MyFunc3 description", myFunc3.Description);
        Assert.Equal(
            [
                new FunctionParameter("param1", "INT64", Optional: false, "Parameter description"),
                new FunctionParameter("param2", "INT64", Optional: false, "Parameter description"),
            ],
            myFunc3.Parameters);

        Assert.True(catalog.TryGetFunction("DaxLib.Sample.MyFunc1", out var myFunc1));
        Assert.Equal("() => 1", myFunc1.Expression);
        Assert.Empty(myFunc1.Parameters);
    }

    /// <summary>
    /// The TMDL deserializer removes the ``` fences and the annotation lines, and keeps the ///
    /// lines as the description.
    /// </summary>
    [Fact]
    public void Create_FencedPackage_ExpressionHasNoTmdlSyntax()
    {
        using var package = FixtureLoader.OpenPackage("DaxPatterns.AbcClassification", "0.1.4");

        var catalog = FunctionCatalog.Create(package);

        Assert.Equal(2, catalog.Functions.Count);
        Assert.All(catalog.Functions, function =>
        {
            Assert.DoesNotContain("```", function.Expression, StringComparison.Ordinal);
            Assert.DoesNotContain("annotation", function.Expression, StringComparison.Ordinal);
            Assert.DoesNotContain("DAXLIB_", function.Expression, StringComparison.Ordinal);
            Assert.StartsWith("(", function.Expression, StringComparison.Ordinal);
            Assert.StartsWith("Return the ", function.Description, StringComparison.Ordinal);
            Assert.Contains("\nArticle: https://www.daxpatterns.com/abc-classification/\n", function.Description, StringComparison.Ordinal);
        });

        Assert.True(catalog.TryGetFunction("DaxPatterns.AbcClassification.ComputeInAbcClass", out var compute));
        Assert.Equal(7, compute.Parameters.Count);
        Assert.All(compute.Parameters, parameter => Assert.Equal("ANYREF EXPR", parameter.Type));
        Assert.Equal("valueExpr", compute.Parameters[0].Name);
    }

    [Fact]
    public void ParseParameters_DefaultsTypesAndComments_AreRead()
    {
        const string expression =
            """
            (
                // The value
                /* to format */
                value : NUMERIC VAL,
                -- Optional
                decimals : INT64 = 2,
                untyped,
                fn : ANYREF EXPR = CALCULATE ( 1, ALL ( T ) )
            ) =>
                value
            """;

        var parameters = FunctionCatalog.ParseParameters(expression);

        Assert.Equal(
            [
                new FunctionParameter("value", "NUMERIC VAL", Optional: false, "The value\nto format"),
                new FunctionParameter("decimals", "INT64", Optional: true, "Optional"),
                new FunctionParameter("untyped", "", Optional: false, null),
                new FunctionParameter("fn", "ANYREF EXPR", Optional: true, null),
            ],
            parameters);
    }

    [Fact]
    public void SuggestNames_Typo_ReturnsTheClosestNames()
    {
        using var package = FixtureLoader.OpenPackage("DaxLib.SVG", "2.0.0");
        var catalog = FunctionCatalog.Create(package);

        var suggestions = catalog.SuggestNames("DaxLib.SVG.Viz.Pil", 10);

        Assert.Equal(10, suggestions.Count);
        Assert.Equal("DaxLib.SVG.Viz.Pill", suggestions[0]);
    }
}
