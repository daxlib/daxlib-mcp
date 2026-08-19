namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// One package on a search page. The property names are the wire contract.
/// </summary>
/// <param name="Downloads">Total downloads across all versions, not just the listed one.</param>
internal sealed record PackageSearchData(
    string Id,
    string Version,
    long Downloads,
    string? Description,
    bool HasReadme);
