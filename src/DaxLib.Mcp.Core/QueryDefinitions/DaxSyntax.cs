using System;
using System.Collections.Generic;
using Dax.Tokenizer;

namespace DaxLib.Mcp.Core.QueryDefinitions;

/// <summary>
/// What the query definitions need from the DAX lexer (<see cref="DaxTokenizer"/>): calls,
/// comments, names and literals. The lexer tells code from strings and comments, so a function
/// name inside a string or a comment is never mistaken for a call.
/// </summary>
/// <remarks>
/// The lexer reads a dotted name as one identifier (<c>DaxLib.SVG.Viz.Pill</c>,
/// <c>NORM.S.DIST</c>), also when a part of it is a reserved word (<c>DaxLib.SVG.Element.Group</c>).
/// A name without dots is an identifier too, or a keyword token when it is a reserved word.
/// </remarks>
internal static class DaxSyntax
{
    /// <summary>Every token, comments and white space included.</summary>
    public static IReadOnlyList<DaxToken> Tokenize(string expression) =>
        DaxTokenizer.Tokenize(expression, DaxLocale.US, includeHidden: true);

    /// <summary>
    /// Returns the names that the expression calls, distinct and in order of first appearance: an
    /// identifier directly followed by <c>(</c>, comments in between ignored. Built-in functions
    /// are included; the caller classifies the names. A reserved word followed by <c>(</c>, such as
    /// <c>NOT (</c> or <c>BLANK ()</c>, is not a call.
    /// </summary>
    public static IReadOnlyList<string> FindCallNames(string expression)
    {
        List<string> names = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (var token in Tokenize(expression))
        {
            if (IsIdentifier(token) && token.Next?.Type == DaxToken.OPEN_PARENS && seen.Add(token.Text))
                names.Add(token.Text);
        }

        return names;
    }

    public static bool IsComment(DaxToken token) =>
        token.Type is DaxToken.SINGLE_LINE_COMMENT or DaxToken.DELIMITED_COMMENT;

    public static bool IsWhiteSpace(DaxToken token) =>
        token.Type is DaxToken.WHITESPACES;

    public static bool IsIdentifier(DaxToken token) =>
        token.Type is DaxToken.TABLE_OR_VARIABLE or DaxToken.OTHER_IDENTIFIER;

    /// <summary>
    /// True for a token that can name a parameter: an identifier, or a word that the lexer reads
    /// as a keyword or a property, such as <c>text</c>, <c>year</c> or <c>description</c>.
    /// </summary>
    public static bool IsName(DaxToken token) =>
        IsIdentifier(token)
        || token.Type is DaxToken.DATATABLE
        || token.Type is > DaxToken.Z__FIRSTKEYWORD__ and < DaxToken.Z__LASTKEYWORD__
        || token.Type is > DaxToken.Z__FIRSTPROPERTY__ and < DaxToken.Z__LASTPROPERTY__;

    /// <summary>
    /// True for a string, a quoted table name or a bracketed name: inside them, white space is
    /// part of the value.
    /// </summary>
    public static bool IsLiteral(DaxToken token) => token.Type
        is DaxToken.STRING_LITERAL
        or DaxToken.TABLE
        or DaxToken.COLUMN_OR_MEASURE
        or DaxToken.UNTERMINATED_STRING
        or DaxToken.UNTERMINATED_TABLEREF
        or DaxToken.UNTERMINATED_COLREF;
}
