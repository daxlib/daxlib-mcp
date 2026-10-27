using System.Linq;
using Dax.Tokenizer;
using DaxLib.Mcp.Core.QueryDefinitions;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.QueryDefinitions;

/// <summary>
/// The parts of the DAX lexer that call detection and parameter parsing rely on.
/// </summary>
public sealed class DaxSyntaxTests
{
    [Fact]
    public void FindCallNames_IdentifierBeforeParenthesis_IsACall()
    {
        var calls = DaxSyntax.FindCallNames("A.F ( 1 ) + B.G( A.F(2) ) + C.H /* not a call */ + D.I /* comment */ (3)");

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
            "A.InString( 1 ) "" A.Escaped( 1 )" & 'A.Quoted( 1 )'[x] & [A.Bracket( 1 )]
            """;

        Assert.Empty(DaxSyntax.FindCallNames(expression));
    }

    [Fact]
    public void FindCallNames_BuiltInFunctions_AreReturnedButKeywordsAreNot()
    {
        var calls = DaxSyntax.FindCallNames("NORM.S.DIST ( 1, TRUE () ) + SUM ( T[c] ) + NOT ( BLANK () )");

        Assert.Equal(["NORM.S.DIST", "SUM"], calls);
    }

    /// <summary>
    /// A reserved name part, rejected by the current DAX parser, still makes one identifier.
    /// </summary>
    [Theory]
    [InlineData("DaxLib.SVG.Element.Group")]
    [InlineData("Ankush.SemanticInfo.Model.Describe")]
    [InlineData("XU.Calendar.Build.Basic")]
    [InlineData("My_Func.With_Underscore.X1")]
    public void FindCallNames_DottedName_IsOneCall(string name)
    {
        Assert.Equal([name], DaxSyntax.FindCallNames($"{name} ( 1 )"));
    }

    [Fact]
    public void Tokenize_KeywordUsedAsParameterName_IsAName()
    {
        var tokens = DaxSyntax.Tokenize("( text : STRING, year : INT64, description : STRING, x : ANYREF EXPR )")
            .Where(token => !DaxSyntax.IsWhiteSpace(token))
            .ToArray();

        string[] names = [.. tokens.Where((token, index) => index + 1 < tokens.Length && tokens[index + 1].Type == DaxToken.COLON).Select(token => token.Text)];

        Assert.Equal(["text", "year", "description", "x"], names);
        Assert.All(tokens.Where(token => names.Contains(token.Text)), token => Assert.True(DaxSyntax.IsName(token)));
    }

    [Theory]
    [InlineData("\"a\nb\"")]
    [InlineData("'Sales Table'")]
    [InlineData("[Sales Amount]")]
    [InlineData("\"never closed")]
    public void Tokenize_Literals_AreLiterals(string text)
    {
        var token = Assert.Single(DaxSyntax.Tokenize(text));

        Assert.True(DaxSyntax.IsLiteral(token));
        Assert.Equal((0, text.Length - 1), (token.StartIndex, token.StopIndex));
    }

    [Theory]
    [InlineData("// a line comment")]
    [InlineData("-- a line comment")]
    [InlineData("/* a\nblock */")]
    public void Tokenize_Comments_AreComments(string text)
    {
        var token = Assert.Single(DaxSyntax.Tokenize(text));

        Assert.True(DaxSyntax.IsComment(token));
        Assert.Equal(text, token.Text);
    }
}
