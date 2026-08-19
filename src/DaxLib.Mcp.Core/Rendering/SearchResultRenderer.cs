using System;
using System.Collections.Generic;
using System.Linq;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;

namespace DaxLib.Mcp.Core.Rendering;

/// <summary>
/// Renders a search result: a short message with the count, and one typed entry per package.
/// </summary>
internal static class SearchResultRenderer
{
    // A page can carry up to 100 entries. Descriptions are truncated so that a long one does not
    // hide the entries around it.
    private const int MaximumDescriptionLength = 500;

    public static ToolOutcome Render(IReadOnlyList<GalleryPackage> packages, SearchRequest request)
    {
        var message = Message(packages.Count, request);
        var data = packages.Select(p => new PackageSearchData(
            Id: p.Id.ToNormalizedString(),
            Version: p.Version.ToNormalizedString(),
            Downloads: p.Downloads,
            Description: Truncate(p.Description),
            HasReadme: p.HasReadme)).ToArray();

        return ToolOutcome.Ok(message, data);
    }

    public static ToolOutcome NoResults(SearchRequest request)
    {
        var message = NoResultsMessage(request);
        var data = Array.Empty<PackageSearchData>();

        return ToolOutcome.Ok(message, data);
    }

    private static string NoResultsMessage(SearchRequest request)
    {
        if (request.Skip > 0)
            return PageBeyondResults(request);

        if (request.Query.Length == 0)
            return EmptyCatalog();

        var noMatch = $"No packages matched `{request.Query}`. ";

        if (!request.IncludePrerelease)
        {
            return noMatch +
                "The search was limited to packages with stable latest versions, so packages whose latest version is a prerelease were excluded. " +
                "If prerelease packages are relevant, ask the user whether to include them before retrying.";
        }

        var firstTerm = FirstTerm(request.Query);

        return firstTerm is not null
            ? noMatch + $"The query is matched as a single substring. For a broader search, retry with one keyword, such as `{firstTerm}`."
            : noMatch + "Retry with a different single keyword, such as a package ID fragment or a domain term like `svg`, `date`, or `format`.";
    }

    private static string PageBeyondResults(SearchRequest request)
    {
        var matching = request.Query.Length == 0 ? string.Empty : $" for `{request.Query}`";

        return $"No packages found on this page{matching} (skip {request.Skip}, limit {request.Limit}). " +
               "The requested page is beyond the end of the result set, so all matching packages have already been returned on earlier pages. " +
               "Do not change the query. Use `skip: 0` only if you need to retrieve the results again.";
    }

    private static string EmptyCatalog() =>
        "The DAX Lib catalog returned no packages for an empty query. This is unexpected because an empty query lists the entire catalog. " +
        "Retry once. If the retry also returns no packages, tell the user that the DAX Lib catalog may be temporarily unavailable or experiencing an unexpected problem.";

    private static string? FirstTerm(string query)
    {
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return terms.Length > 1 ? terms[0] : null;
    }

    private static string Message(int count, SearchRequest request)
    {
        var noun = count == 1 ? "package" : "packages";
        var matching = request.Query.Length == 0 ? string.Empty : $" matching `{request.Query}`";
        var range = request.Skip == 0 ? string.Empty : $" (results {request.Skip + 1}–{request.Skip + count})";

        // The gallery reports no total, so a short page is the only end-of-results signal available.
        var paging = count < request.Limit
            ? "There are no further pages."
            : $"More results may exist: call again with `skip: {request.Skip + request.Limit}` for the next page.";

        return $"Found {count} {noun}{matching}{range}. {paging}";
    }

    // A description is free text from a package author: it can be null, empty, or arbitrarily
    // long, and the gallery DTO does not enforce its non-nullable annotation at runtime.
    private static string? Truncate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalizedText = text.ReplaceLineEndings(" ").Trim();

        if (normalizedText.Length > MaximumDescriptionLength)
            normalizedText = string.Concat(normalizedText.AsSpan(0, MaximumDescriptionLength), "…");

        return normalizedText;
    }
}
