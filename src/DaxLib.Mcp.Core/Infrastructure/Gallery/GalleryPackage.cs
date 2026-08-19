using System;
using System.Collections.Generic;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.Infrastructure.Gallery;

/// <summary>
/// A package version as returned by the gallery API.
/// </summary>
internal sealed record GalleryPackage
{
    public required PackageId Id { get; init; }
    public required PackageVersion Version { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Authors { get; init; }
    public required IReadOnlyList<string> Owners { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required string ReleaseNotes { get; init; }
    public Uri? ProjectUrl { get; init; }
    public Uri? RepositoryUrl { get; init; }
    public required DateTimeOffset Published { get; init; }
    public required long Downloads { get; init; }
    public required bool HasReadme { get; init; }
    public Uri GalleryUrl => new($"https://daxlib.org/package/{Id.ToNormalizedString()}/{Version.ToNormalizedString()}");
}
