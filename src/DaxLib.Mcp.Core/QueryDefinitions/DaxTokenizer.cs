using System;
using System.Collections.Generic;

namespace DaxLib.Mcp.Core.QueryDefinitions;

internal enum DaxTokenKind
{
    Comment,
    String,
    QuotedName,
    BracketName,
    Identifier,
    Number,
    Operator,
}

/// <param name="Start">The offset of the token in the scanned text.</param>
internal readonly record struct DaxToken(DaxTokenKind Kind, string Text, int Start);

/// <summary>
/// A lexical scanner for DAX expressions, precise enough to tell code from strings and comments.
/// It is not a parser: it only splits the text into tokens, so a function name inside a string or
/// a comment is never mistaken for a call.
/// </summary>
/// <remarks>
/// The rules, tried in this order at each position: white space (skipped); a line comment
/// (<c>//</c> or <c>--</c>) or a block comment; a string with <c>""</c> as escape; a quoted name
/// with <c>''</c> as escape; a bracket name with <c>]]</c> as escape; an identifier, whose dotted
/// parts belong to it (<c>DaxLib.SVG.Viz.Pill</c> is one token); a number; any other character as
/// a one-character operator. An unterminated comment, string or name runs to the end of the text.
/// </remarks>
internal static class DaxTokenizer
{
    public static IReadOnlyList<DaxToken> Tokenize(string text)
    {
        List<DaxToken> tokens = [];
        var position = 0;

        while (position < text.Length)
        {
            var current = text[position];

            if (char.IsWhiteSpace(current))
            {
                position++;
                continue;
            }

            var start = position;
            DaxTokenKind kind;

            if (StartsWith(text, position, "//") || StartsWith(text, position, "--"))
            {
                kind = DaxTokenKind.Comment;
                position = text.IndexOf('\n', position);
                if (position < 0)
                    position = text.Length;
            }
            else if (StartsWith(text, position, "/*"))
            {
                kind = DaxTokenKind.Comment;
                position = text.IndexOf("*/", position + 2, StringComparison.Ordinal);
                position = position < 0 ? text.Length : position + 2;
            }
            else if (current is '"')
            {
                kind = DaxTokenKind.String;
                position = SkipDelimited(text, position, '"');
            }
            else if (current is '\'')
            {
                kind = DaxTokenKind.QuotedName;
                position = SkipDelimited(text, position, '\'');
            }
            else if (current is '[')
            {
                kind = DaxTokenKind.BracketName;
                position = SkipDelimited(text, position, ']');
            }
            else if (IsIdentifierStart(current))
            {
                kind = DaxTokenKind.Identifier;
                position = SkipIdentifier(text, position);
            }
            else if (char.IsAsciiDigit(current))
            {
                kind = DaxTokenKind.Number;
                position = SkipNumber(text, position);
            }
            else
            {
                kind = DaxTokenKind.Operator;
                position++;
            }

            // A comment runs to the end of the line; its line break, a \r included, is white space.
            var token = text[start..position];
            tokens.Add(new DaxToken(kind, kind is DaxTokenKind.Comment ? token.TrimEnd() : token, start));
        }

        return tokens;
    }

    /// <summary>
    /// Returns the names that the expression calls, distinct and in order of first appearance: an
    /// identifier directly followed by <c>(</c>, comments in between ignored. Built-in functions
    /// are included; the caller classifies the names.
    /// </summary>
    public static IReadOnlyList<string> FindCallNames(string expression)
    {
        List<string> names = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        DaxToken? previous = null;

        foreach (var token in Tokenize(expression))
        {
            if (token.Kind is DaxTokenKind.Comment)
                continue;

            if (previous is { Kind: DaxTokenKind.Identifier } identifier
                && token is { Kind: DaxTokenKind.Operator, Text: "(" }
                && seen.Add(identifier.Text))
            {
                names.Add(identifier.Text);
            }

            previous = token;
        }

        return names;
    }

    private static bool StartsWith(string text, int position, string value) =>
        string.CompareOrdinal(text, position, value, 0, value.Length) == 0;

    // A doubled closing delimiter is an escape and does not end the token.
    private static int SkipDelimited(string text, int position, char closing)
    {
        position++;

        while (position < text.Length)
        {
            if (text[position] != closing)
            {
                position++;
                continue;
            }

            if (position + 1 < text.Length && text[position + 1] == closing)
            {
                position += 2;
                continue;
            }

            return position + 1;
        }

        return text.Length;
    }

    private static int SkipIdentifier(string text, int position)
    {
        position = SkipIdentifierPart(text, position);

        while (position + 1 < text.Length && text[position] == '.' && IsIdentifierStart(text[position + 1]))
            position = SkipIdentifierPart(text, position + 1);

        return position;
    }

    private static int SkipIdentifierPart(string text, int position)
    {
        position++;

        while (position < text.Length && (IsIdentifierStart(text[position]) || char.IsAsciiDigit(text[position])))
            position++;

        return position;
    }

    // \d+(\.\d*)?([eE][+-]?\d+)?
    private static int SkipNumber(string text, int position)
    {
        position = SkipDigits(text, position);

        if (position < text.Length && text[position] == '.')
            position = SkipDigits(text, position + 1);

        if (position < text.Length && text[position] is 'e' or 'E')
        {
            var exponent = position + 1;

            if (exponent < text.Length && text[exponent] is '+' or '-')
                exponent++;

            if (exponent < text.Length && char.IsAsciiDigit(text[exponent]))
                position = SkipDigits(text, exponent);
        }

        return position;
    }

    private static int SkipDigits(string text, int position)
    {
        while (position < text.Length && char.IsAsciiDigit(text[position]))
            position++;

        return position;
    }

    private static bool IsIdentifierStart(char character) =>
        char.IsAsciiLetter(character) || character == '_';
}
