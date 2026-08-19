using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.Rendering;

internal static class PackageLookupRenderer
{
    public static ToolOutcome Render(PackageLookupFailure failure)
    {
        return failure switch
        {
            PackageLookupFailure.UnknownPackage(var requestedId, var suggestions) => PackageNotFound(requestedId, suggestions),
            PackageLookupFailure.UnknownVersion(var id, var requestedVersion, var availableVersions) => VersionNotFound(id, requestedVersion, availableVersions),
            _ => throw new UnreachableException($"Unhandled {nameof(PackageLookupFailure)} type '{failure.GetType()}'."),
        };
    }

    private static ToolOutcome PackageNotFound(
        string requestedId,
        IReadOnlyList<PackageId> suggestions)
    {
        string[] names = [.. suggestions.Select(id => id.ToNormalizedString())];

        return ToolOutcome.Failed(names.Length > 0
            ? $"No DAX Lib package with id '{requestedId}' exists. " +
              $"Possible matches: {string.Join(", ", names)}. " +
              $"Call this tool again with one of these ids, or use `{ToolNames.SearchPackages}` to find the right package."
            : $"No DAX Lib package with id '{requestedId}' exists. " +
              $"Use `{ToolNames.SearchPackages}` to find the right package.");
    }

    private static ToolOutcome VersionNotFound(
        PackageId id,
        string requestedVersion,
        IReadOnlyList<PackageVersion> availableVersions)
    {
        var packageId = id.ToNormalizedString();

        return ToolOutcome.Failed(availableVersions.Count > 0
            ? $"Package {packageId} does not have version '{requestedVersion}'. " +
              $"Available versions: {string.Join(", ", VersionListRenderer.Render(availableVersions))}. " +
              "Call this tool again with one of the available versions."
            : $"Package {packageId} does not have version '{requestedVersion}'. " +
              $"Use `{ToolNames.GetPackage}` to retrieve the package's published versions.");
    }
}
