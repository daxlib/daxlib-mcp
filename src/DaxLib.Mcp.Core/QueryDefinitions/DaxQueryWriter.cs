using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DaxLib.Mcp.Core.QueryDefinitions;

/// <summary>
/// Writes functions as the DEFINE block of a DAX query.
/// </summary>
/// <remarks>
/// The conversion from TMDL is the minimum the engine needs: <c>DEFINE</c> before the first
/// function, and the name without quotes. The TMDL deserializer has already removed the fences and
/// the annotations. Names and expressions are never changed: a published name that the DAX parser
/// rejects is returned as it is, and the query error points to it.
/// </remarks>
internal static class DaxQueryWriter
{
    private const string FunctionIndent = "    ";
    private const string ExpressionIndent = "        ";

    public static string Write(IEnumerable<CatalogFunction> functions, bool includeDescriptions)
    {
        var builder = new StringBuilder("DEFINE");

        foreach (var function in functions)
        {
            if (includeDescriptions && function.Description is { } description)
            {
                foreach (var line in description.Split('\n'))
                {
                    var text = line.Trim();
                    AppendLine(builder, text.Length == 0 ? $"{FunctionIndent}//" :$"{FunctionIndent}// {text}");
                }
            }

            AppendLine(builder, $"{FunctionIndent}FUNCTION {function.Name} =");
            AppendExpression(builder, function.Expression);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Indents every line of the expression, except the empty lines and the lines that continue a
    /// string or a quoted name: there, white space is part of the value.
    /// </summary>
    private static void AppendExpression(StringBuilder builder, string expression)
    {
        var literals = DaxTokenizer.Tokenize(expression)
            .Where(token => token.Kind is DaxTokenKind.String or DaxTokenKind.QuotedName or DaxTokenKind.BracketName
                && token.Text.Contains('\n', System.StringComparison.Ordinal))
            .ToArray();

        var lineStart = 0;

        foreach (var line in expression.Split('\n'))
        {
            var insideLiteral = literals.Any(token => token.Start < lineStart && lineStart < token.Start + token.Text.Length);

            if (line.Length == 0 || insideLiteral)
                AppendLine(builder, line);
            else
                AppendLine(builder, ExpressionIndent + line);

            lineStart += line.Length + 1;
        }
    }

    private static void AppendLine(StringBuilder builder, string line) =>
        builder.Append('\n').Append(line);
}
