using System;
using System.Collections.Generic;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// Data returned by <c>get_package</c>. The property names are the wire contract. Never contains
/// DAX code.
/// </summary>
/// <param name="Downloads">Downloads of this version, not the all-version total a search reports.</param>
/// <param name="Versions">Every published version, newest first.</param>
internal sealed record PackageDetailsData(
    string Id,
    string Version,
    string? Description,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Owners,
    IReadOnlyList<string> Tags,
    DateTimeOffset Published,
    long Downloads,
    bool HasReadme,
    string GalleryUrl,
    string? ProjectUrl,
    string? RepositoryUrl,
    IReadOnlyList<string> Versions,
    string? ReleaseNotes);
