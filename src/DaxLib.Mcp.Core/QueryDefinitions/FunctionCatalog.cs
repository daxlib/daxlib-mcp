using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dax.Tokenizer;
using DaxLib.Mcp.Core.SdkModeling;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.QueryDefinitions;

/// <summary>
/// The functions of one package version, read from its TMDL, with what a DEFINE block needs:
/// expression, description, parameters, and the names each function calls.
/// </summary>
internal sealed class FunctionCatalog
{
    private readonly Dictionary<string, CatalogFunction> _byName;

    private FunctionCatalog(
        string packageId,
        PackageVersion version,
        IReadOnlyList<PackageDependency> dependencies,
        IReadOnlyList<CatalogFunction> functions)
    {
        PackageId = packageId;
        Version = version;
        Dependencies = dependencies;
        Functions = functions;
        _byName = functions.ToDictionary(function => function.Name, StringComparer.OrdinalIgnoreCase);
    }

    public string PackageId { get; }

    public PackageVersion Version { get; }

    /// <summary>The dependencies the manifest declares, each at an exact version.</summary>
    public IReadOnlyList<PackageDependency> Dependencies { get; }

    /// <summary>Every function, in the order of the TMDL document.</summary>
    public IReadOnlyList<CatalogFunction> Functions { get; }

    /// <summary>Finds a function by name, case-insensitively.</summary>
    public bool TryGetFunction(string name, [NotNullWhen(true)] out CatalogFunction? function) =>
        _byName.TryGetValue(name, out function);

    /// <summary>
    /// The function names closest to <paramref name="name"/> by edit distance, case-insensitive.
    /// </summary>
    public IReadOnlyList<string> SuggestNames(string name, int maximum) =>
    [
        .. Functions
            .Select(function => (function.Name, Distance: EditDistance(name.ToUpperInvariant(), function.Name.ToUpperInvariant())))
            .OrderBy(candidate => candidate.Distance)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maximum)
            .Select(candidate => candidate.Name),
    ];

    private static int EditDistance(string source, string target)
    {
        var previous = new int[target.Length + 1];
        var current = new int[target.Length + 1];

        for (var j = 0; j <= target.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= target.Length; j++)
            {
                var substitution = previous[j - 1] + (source[i - 1] == target[j - 1] ? 0 : 1);
                current[j] = Math.Min(substitution, Math.Min(previous[j], current[j - 1]) + 1);
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Length];
    }

    /// <summary>
    /// Reads the package TMDL. Throws when the TMDL cannot be parsed.
    /// </summary>
    public static FunctionCatalog Create(Package package)
    {
        var manifest = package.Manifest;
        var model = PackageTmdlSerializer.DeserializeModel(package);

        CatalogFunction[] functions =
        [
            .. model.Functions.Select(function => CreateFunction(function.Name, function.Expression, function.Description)),
        ];

        return new FunctionCatalog(
            manifest.PackageId.ToNormalizedString(),
            manifest.Version,
            [.. manifest.Dependencies],
            functions);
    }

    internal static CatalogFunction CreateFunction(string name, string? expression, string? description)
    {
        // TOM removes the ``` fences and the annotation lines, and keeps the /// text as the
        // description. Line endings are normalized, so the DEFINE block is the same on every platform.
        var normalizedExpression = (expression ?? string.Empty).ReplaceLineEndings("\n").Trim('\n');
        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.ReplaceLineEndings("\n").Trim('\n');

        return new CatalogFunction(
            name,
            normalizedExpression,
            normalizedDescription,
            ParseParameters(normalizedExpression),
            DaxSyntax.FindCallNames(normalizedExpression));
    }

    /// <summary>
    /// Reads the parameter list <c>( p1 : TYPE [= default], ... ) =></c> at the start of the
    /// expression. The comment lines just before a parameter are its description.
    /// </summary>
    internal static IReadOnlyList<FunctionParameter> ParseParameters(string expression)
    {
        DaxToken[] tokens = [.. DaxSyntax.Tokenize(expression).Where(token => !DaxSyntax.IsWhiteSpace(token))];
        var index = 0;

        while (index < tokens.Length && DaxSyntax.IsComment(tokens[index]))
            index++;

        if (index >= tokens.Length || tokens[index].Type != DaxToken.OPEN_PARENS)
            return [];

        List<FunctionParameter> parameters = [];
        List<DaxToken> segment = [];
        var depth = 0;

        for (index++; index < tokens.Length; index++)
        {
            var token = tokens[index];

            if (token.Type == DaxToken.OPEN_PARENS)
            {
                depth++;
            }
            else if (token.Type == DaxToken.CLOSE_PARENS)
            {
                if (depth == 0)
                    break;

                depth--;
            }
            else if (depth == 0 && token.Type == DaxToken.COMMA)
            {
                AddParameter(parameters, segment);
                segment.Clear();
                continue;
            }

            segment.Add(token);
        }

        AddParameter(parameters, segment);

        return parameters;
    }

    private static void AddParameter(List<FunctionParameter> parameters, List<DaxToken> segment)
    {
        var nameIndex = segment.FindIndex(token => !DaxSyntax.IsComment(token));

        if (nameIndex < 0 || !DaxSyntax.IsName(segment[nameIndex]))
            return;

        string[] comments = [.. segment.Take(nameIndex).Select(token => CommentText(token.Text)).Where(text => text.Length > 0)];
        List<string> typeParts = [];
        var optional = false;
        var index = nameIndex + 1;

        if (index < segment.Count && segment[index].Type == DaxToken.COLON)
        {
            for (index++; index < segment.Count; index++)
            {
                var token = segment[index];

                if (DaxSyntax.IsComment(token))
                    continue;

                if (token.Type == DaxToken.EQ)
                    break;

                typeParts.Add(token.Text);
            }
        }

        for (; index < segment.Count; index++)
        {
            if (segment[index].Type == DaxToken.EQ)
            {
                optional = true;
                break;
            }
        }

        parameters.Add(new FunctionParameter(
            segment[nameIndex].Text,
            string.Join(' ', typeParts),
            optional,
            comments.Length == 0 ? null : string.Join("\n", comments)));
    }

    private static string CommentText(string comment)
    {
        if (comment.StartsWith("/*", StringComparison.Ordinal))
        {
            var end = comment.EndsWith("*/", StringComparison.Ordinal) && comment.Length >= 4 ? comment.Length - 2 : comment.Length;
            return comment[2..end].Trim();
        }

        // Line comments: //, /// and --.
        return comment.TrimStart('/', '-').Trim();
    }
}

/// <summary>A function of a <see cref="FunctionCatalog"/>.</summary>
/// <param name="Expression">The DAX expression as published, without TMDL syntax.</param>
/// <param name="CallNames">
/// Every name the expression calls, built-in functions included, in order of first appearance.
/// </param>
internal sealed record CatalogFunction(
    string Name,
    string Expression,
    string? Description,
    IReadOnlyList<FunctionParameter> Parameters,
    IReadOnlyList<string> CallNames);

/// <param name="Type">The declared type, e.g. <c>INT64</c> or <c>ANYREF EXPR</c>; empty when none is declared.</param>
/// <param name="Optional">True when the parameter has a default value.</param>
internal sealed record FunctionParameter(string Name, string Type, bool Optional, string? Description);
