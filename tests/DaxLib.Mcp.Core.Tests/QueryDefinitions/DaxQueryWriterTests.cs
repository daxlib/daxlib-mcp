using System;
using System.Linq;
using System.Text.RegularExpressions;
using DaxLib.Mcp.Core.QueryDefinitions;
using DaxLib.Mcp.Core.Tests.Fixtures;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.QueryDefinitions;

public sealed partial class DaxQueryWriterTests
{
    /// <summary>
    /// T02 is the converted DaxLib.Sample that ran on dax.do and returned <c>1, 2.5, 3</c>.
    /// </summary>
    [Fact]
    public void Write_Sample_EqualsTheQueryThatRanOnTheEngine()
    {
        using var package = FixtureLoader.OpenPackage("DaxLib.Sample", "0.1.8");
        var catalog = FunctionCatalog.Create(package);

        var block = DaxQueryWriter.Write(catalog.Functions, includeDescriptions: true);

        var golden = FixtureLoader.ReadText("Expected.T02_converted_unquoted.dax");
        var expected = golden[..golden.IndexOf("EVALUATE", StringComparison.Ordinal)];

        Assert.Equal(CollapseWhiteSpace(expected), CollapseWhiteSpace(block));
        Assert.Equal(497, block.Length);
    }

    [Fact]
    public void Write_FencedPackage_HasNoQuotesAnnotationsOrFences()
    {
        using var package = FixtureLoader.OpenPackage("DaxPatterns.AbcClassification", "0.1.4");
        var catalog = FunctionCatalog.Create(package);

        var block = DaxQueryWriter.Write(catalog.Functions, includeDescriptions: true);

        Assert.StartsWith("DEFINE\n    // Return the value computed", block, StringComparison.Ordinal);
        Assert.Contains("\n    FUNCTION DaxPatterns.AbcClassification.ComputeInAbcClass =\n        (", block, StringComparison.Ordinal);
        Assert.DoesNotContain("'DaxPatterns", block, StringComparison.Ordinal);
        Assert.DoesNotContain("annotation", block, StringComparison.Ordinal);
        Assert.DoesNotContain("```", block, StringComparison.Ordinal);
        Assert.DoesNotContain("///", block, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_WithoutDescriptions_WritesNoComments()
    {
        var function = FunctionCatalog.CreateFunction("A.F", "() => 1", "Returns one");

        Assert.Equal("DEFINE\n    FUNCTION A.F =\n        () => 1", DaxQueryWriter.Write([function], includeDescriptions: false));
        Assert.Equal("DEFINE\n    // Returns one\n    FUNCTION A.F =\n        () => 1", DaxQueryWriter.Write([function], includeDescriptions: true));
    }

    /// <summary>
    /// A name part that the current DAX parser rejects (<c>Group</c>) is written as published.
    /// </summary>
    [Fact]
    public void Write_BlockedNamePart_IsNotRenamed()
    {
        var function = FunctionCatalog.CreateFunction("DaxLib.SVG.Element.Group", "( x : STRING ) => x", null);

        Assert.Contains("FUNCTION DaxLib.SVG.Element.Group =", DaxQueryWriter.Write([function], includeDescriptions: true), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_MultiLineString_KeepsTheStringUnchanged()
    {
        var function = FunctionCatalog.CreateFunction("A.F", "() =>\n    \"first\n  second\"\n    & \"x\"", null);

        var block = DaxQueryWriter.Write([function], includeDescriptions: false);

        Assert.Equal("DEFINE\n    FUNCTION A.F =\n        () =>\n            \"first\n  second\"\n            & \"x\"", block);
    }

    [Fact]
    public void Write_CrLfExpression_UsesLineFeeds()
    {
        var function = FunctionCatalog.CreateFunction("A.F", "(\r\n    x : INT64\r\n) =>\r\n    x", "Line one\r\nLine two");

        var block = DaxQueryWriter.Write([function], includeDescriptions: true);

        Assert.DoesNotContain('\r', block);
        Assert.Equal(
            "DEFINE\n    // Line one\n    // Line two\n    FUNCTION A.F =\n        (\n            x : INT64\n        ) =>\n            x",
            block);
    }

    private static string CollapseWhiteSpace(string text) => WhiteSpace().Replace(text, " ").Trim();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();
}
