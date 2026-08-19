using System;
using System.Linq;
using DaxLib.Mcp.Core.SdkModeling;
using DaxLib.Mcp.Core.Tests.Fixtures;
using DaxLib.Packaging;
using Microsoft.AnalysisServices.Tabular;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.SdkModeling;

/// <summary>
/// Parses real published <c>functions.tmdl</c> documents, which cover the TMDL grammar edge cases:
/// fenced expressions, comments inside expressions, two-word parameter types.
/// </summary>
public sealed class PackageTmdlSerializerTests
{
    [Theory]
    [InlineData("eg.base64.functions.tmdl", "EG.Base64")]
    [InlineData("bacci.time.functions.tmdl", "Bacci.Time")]
    [InlineData("daxlib.svg.functions.tmdl", "DaxLib.SVG")]
    [InlineData("powerofbi.ibcs.functions.tmdl", "PowerofBI.IBCS")]
    [InlineData("pql.assert.functions.tmdl", "PQL.Assert")]
    [InlineData("xu.tmdl.timeintelligence.functions.tmdl", "XU.TMDL.TimeIntelligence")]
    public void DeserializeModel_PublishedFixture_ParsesEveryFunctionWithItsAnnotations(string fixture, string packageId)
    {
        var model = Deserialize(fixture);

        Assert.NotEmpty(model.Functions);

        // The fixture declares one top-level `function` per function; the parser must account for
        // every one of them, or a package would install incompletely.
        var declared = CountDeclaredFunctions(FixtureLoader.ReadText(fixture));
        Assert.Equal(declared, model.Functions.Count);

        Assert.All(model.Functions, function =>
        {
            Assert.StartsWith($"{packageId}.", function.Name, StringComparison.Ordinal);
            Assert.Equal(packageId, function.Annotations.Find(PackageId.AnnotationName)?.Value);
            Assert.NotNull(function.Annotations.Find(PackageVersion.AnnotationName)?.Value);
            Assert.False(string.IsNullOrWhiteSpace(function.Expression));
        });
    }

    [Fact]
    public void DeserializeModel_Ibcs_PreservesCommentsInsideExpressions()
    {
        var model = Deserialize("powerofbi.ibcs.functions.tmdl");

        // The remote proof of concept stripped `//` comments when the payload crossed the LLM
        // boundary: installing this package that way lost all 873 of them. In-process parsing must
        // keep every one inside the parsed expressions.
        // (`///` lines are TMDL doc-comments — they become the function description, not part of
        // the expression, so they are excluded on both sides.)
        var parsedCommentLines = model.Functions
            .Sum(function => function.Expression.Split('\n').Count(IsExpressionComment));

        var fixtureCommentLines = FixtureLoader.ReadText("powerofbi.ibcs.functions.tmdl")
            .Split('\n')
            .Count(IsExpressionComment);

        Assert.Equal(873, fixtureCommentLines);
        Assert.Equal(fixtureCommentLines, parsedCommentLines);

        static bool IsExpressionComment(string line)
        {
            var trimmed = line.TrimStart();

            return trimmed.StartsWith("//", StringComparison.Ordinal)
                && !trimmed.StartsWith("///", StringComparison.Ordinal);
        }
    }

    private static Model Deserialize(string fixture)
    {
        using var content = FixtureLoader.Open(fixture);

        return PackageTmdlSerializer.DeserializeModel(
            [LibraryDocument.Create(PackagingConstants.TmdlFunctionsFilePath, content)]);
    }

    private static int CountDeclaredFunctions(string tmdl)
    {
        return tmdl.Split('\n').Count(line => line.StartsWith("function ", StringComparison.Ordinal));
    }
}
