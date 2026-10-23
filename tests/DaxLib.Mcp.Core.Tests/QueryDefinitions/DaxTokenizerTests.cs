using System.Linq;
using DaxLib.Mcp.Core.QueryDefinitions;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.QueryDefinitions;

public sealed class DaxTokenizerTests
{
    [Theory]
    [InlineData("\"a \"\"quoted\"\" word\"", nameof(DaxTokenKind.String))]
    [InlineData("'Sales Table'", nameof(DaxTokenKind.QuotedName))]
    [InlineData("'O''Brien'", nameof(DaxTokenKind.QuotedName))]
    [InlineData("[Sales Amount]", nameof(DaxTokenKind.BracketName))]
    [InlineData("[a]]b]", nameof(DaxTokenKind.BracketName))]
    [InlineData("DaxLib.SVG.Viz.Pill", nameof(DaxTokenKind.Identifier))]
    [InlineData("_private1", nameof(DaxTokenKind.Identifier))]
    [InlineData("42", nameof(DaxTokenKind.Number))]
    [InlineData("3.14", nameof(DaxTokenKind.Number))]
    [InlineData("1.5e-3", nameof(DaxTokenKind.Number))]
    [InlineData("// a line comment", nameof(DaxTokenKind.Comment))]
    [InlineData("-- a line comment", nameof(DaxTokenKind.Comment))]
    [InlineData("/* a\nblock */", nameof(DaxTokenKind.Comment))]
    public void Tokenize_SingleToken_ReadsItWhole(string text, string kind)
    {
        var token = Assert.Single(DaxTokenizer.Tokenize(text));

        Assert.Equal(kind, token.Kind.ToString());
        Assert.Equal(text, token.Text);
    }

    [Fact]
    public void Tokenize_Expression_SplitsOperatorsAndSkipsWhiteSpace()
    {
        var tokens = DaxTokenizer.Tokenize("( x : INT64 ) =>\r\n\tx+1 // done\r\n");

        Assert.Equal(
            ["(", "x", ":", "INT64", ")", "=", ">", "x", "+", "1", "// done"],
            tokens.Select(token => token.Text));
        Assert.Equal(DaxTokenKind.Comment, tokens[^1].Kind);
    }

    [Fact]
    public void Tokenize_TokenStart_IsTheOffsetInTheText()
    {
        var tokens = DaxTokenizer.Tokenize("a  \"b\"");

        Assert.Equal([0, 3], tokens.Select(token => token.Start));
    }

    [Theory]
    [InlineData("\"never closed")]
    [InlineData("'never closed")]
    [InlineData("/* never closed")]
    public void Tokenize_Unterminated_RunsToTheEnd(string text)
    {
        var token = Assert.Single(DaxTokenizer.Tokenize(text));

        Assert.Equal(text, token.Text);
    }

    [Fact]
    public void Tokenize_DotNotFollowedByLetter_EndsTheIdentifier()
    {
        var tokens = DaxTokenizer.Tokenize("A.B.1");

        Assert.Equal(["A.B", ".", "1"], tokens.Select(token => token.Text));
    }

    [Fact]
    public void FindCallNames_IdentifierBeforeParenthesis_IsACall()
    {
        var calls = DaxTokenizer.FindCallNames("A.F ( 1 ) + B.G( A.F(2) ) + C.H /* not a call */ + D.I /* comment */ (3)");

        Assert.Equal(["A.F", "B.G", "D.I"], calls);
    }

    [Fact]
    public void FindCallNames_NamesInStringsAndComments_AreNotCalls()
    {
        const string expression =
            """
            // A.Commented( 1 )
            -- A.Dashed( 1 )
            /* A.Block( 1 ) */
            "A.InString( 1 )" & 'A.Quoted'[x] & [A.Bracket( 1 )]
            """;

        Assert.Empty(DaxTokenizer.FindCallNames(expression));
    }

    [Fact]
    public void FindCallNames_BuiltInFunctions_AreReturned()
    {
        var calls = DaxTokenizer.FindCallNames("NORM.S.DIST ( 1, TRUE ) + SUM ( T[c] )");

        Assert.Equal(["NORM.S.DIST", "SUM"], calls);
    }
}
