using System.Collections.Generic;
using System.Linq;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.Rendering;

/// <summary>
/// Renders the details of one package version: a short message with the routing, and the card
/// itself as typed data.
/// </summary>
internal static class PackageCardRenderer
{
    public static ToolOutcome Render(GalleryPackage package, IReadOnlyList<PackageVersion> versions)
    {
        var message = Message(package);
        var data = new PackageDetailsData(
            Id: package.Id.ToNormalizedString(),
            Version: package.Version.ToNormalizedString(),
            Description: string.IsNullOrEmpty(package.Description) ? null : package.Description,
            Authors: package.Authors,
            Owners: package.Owners,
            Tags: package.Tags,
            Published: package.Published,
            Downloads: package.Downloads,
            HasReadme: package.HasReadme,
            GalleryUrl: package.GalleryUrl.ToString(),
            ProjectUrl: package.ProjectUrl?.ToString(),
            RepositoryUrl: package.RepositoryUrl?.ToString(),
            Versions: [.. versions.Select(version => version.ToNormalizedString())],
            ReleaseNotes: string.IsNullOrEmpty(package.ReleaseNotes) ? null : package.ReleaseNotes);

        return ToolOutcome.Ok(message, data);
    }

    private static string Message(GalleryPackage package)
    {
        var readme = package.HasReadme
            ? $"The author's README is available — call `{ToolNames.GetPackageReadme}`."
            : "This version publishes no README.";

        return $"{package.Id.ToNormalizedString()} {package.Version.ToNormalizedString()}: {readme}";
    }
}
