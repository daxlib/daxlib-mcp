using System.Collections.Generic;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.PackageLookup;

/// <summary>
/// The reason a <see cref="PackageResolution"/> could not resolve a package lookup.
/// </summary>
internal abstract record PackageLookupFailure
{
    private PackageLookupFailure()
    {
    }

    /// <summary>The requested package id does not exist.</summary>
    internal sealed record UnknownPackage(
        string RequestedId,
        IReadOnlyList<PackageId> Suggestions) : PackageLookupFailure;

    /// <summary>The requested package version does not exist.</summary>
    internal sealed record UnknownVersion(
        PackageId Id,
        string RequestedVersion,
        IReadOnlyList<PackageVersion> AvailableVersions) : PackageLookupFailure;
}
