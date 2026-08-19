using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Packaging;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.PackageLookup;

/// <summary>
/// Resolves a package id and version into a real package version, or a reason why it could not be found.
/// </summary>
internal sealed partial class PackageResolver(IGalleryClient galleryClient, ILogger<PackageResolver> logger)
{
    private const int MaximumSuggestions = 3;
    private const int MinimumSuggestionTermLength = 3;

    private readonly IGalleryClient _galleryClient = galleryClient;
    private readonly ILogger<PackageResolver> _logger = logger;

    public async Task<PackageResolution> ResolveAsync(string id, string? version, CancellationToken cancellationToken)
    {
        // Tool arguments are deserialized from client JSON, so a non-nullable parameter can still arrive null.
        if (!PackageId.TryParse(id, out var packageId))
        {
            return await UnknownPackageAsync(id, cancellationToken);
        }

        var versions = await _galleryClient.GetVersionsAsync(packageId, cancellationToken);

        PackageVersion targetVersion;

        if (string.IsNullOrWhiteSpace(version))
        {
            if (versions.Count == 0)
                return await UnknownPackageAsync(id, cancellationToken);

            targetVersion = versions[0];
        }
        else
        {
            if (!PackageVersion.TryParse(version, out var requestedVersion))
                return UnknownVersion(packageId, version, versions);

            targetVersion = requestedVersion;
        }

        // Fetched by exact id and version, so an unlisted-but-addressable version still resolves.
        var package = await _galleryClient.GetPackageAsync(packageId, targetVersion, cancellationToken);

        if (package is not null)
            return Resolved(package, versions);

        if (versions.Count == 0)
            return await UnknownPackageAsync(id, cancellationToken);

        return UnknownVersion(packageId, targetVersion.ToNormalizedString(), versions);
    }

    private PackageResolution Resolved(GalleryPackage package, IReadOnlyList<PackageVersion> versions)
    {
        LogPackageResolutionCompleted(package.Id, package.Version, versions.Count);

        return PackageResolution.Resolved(package, versions);
    }

    private async Task<PackageResolution> UnknownPackageAsync( string requestedId, CancellationToken cancellationToken)
    {
        var suggestions = await SearchForSimilarPackagesAsync(requestedId, cancellationToken);
        LogUnknownPackage(requestedId, suggestions.Count);

        return PackageResolution.Unresolved(
            new PackageLookupFailure.UnknownPackage(requestedId, suggestions));
    }

    private PackageResolution UnknownVersion(PackageId packageId, string requestedVersion, IReadOnlyList<PackageVersion> versions)
    {
        LogUnknownVersion(packageId, requestedVersion, versions.Count);

        return PackageResolution.Unresolved(
            new PackageLookupFailure.UnknownVersion(packageId, requestedVersion, versions));
    }

    private async Task<IReadOnlyList<PackageId>> SearchForSimilarPackagesAsync(string requestedId, CancellationToken cancellationToken)
    {
        var term = GetMostDistinctiveSegment(requestedId);

        if (term.Length < MinimumSuggestionTermLength)
            return [];

        var candidates = await _galleryClient.SearchPackagesAsync(
            new SearchRequest(term, Skip: 0, Limit: MaximumSuggestions, IncludePrerelease: true), cancellationToken);

        return [.. candidates.Select(candidate => candidate.Id)];
    }

    private static string GetMostDistinctiveSegment(string requestedId)
    {
        var segments = requestedId.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
            return requestedId.Trim();

        for (var index = segments.Length - 1; index >= 0; index--)
        {
            if (segments[index].Length >= MinimumSuggestionTermLength)
                return segments[index];
        }

        return segments[^1];
    }
}
