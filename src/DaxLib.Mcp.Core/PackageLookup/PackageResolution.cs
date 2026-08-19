using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.PackageLookup;

/// <summary>
/// The result of a package lookup, either resolved or unresolved. An unresolved lookup is not an
/// error: it is a valid result the caller renders to the user.
/// </summary>
internal sealed record PackageResolution
{
    private PackageResolution()
    {
    }

    public GalleryPackage? Package { get; private init; }

    /// <summary>Every listed version of the package, newest first.</summary>
    public IReadOnlyList<PackageVersion> Versions { get; private init; } = [];

    /// <summary>Why the lookup did not resolve, for the caller to render.</summary>
    public PackageLookupFailure? Failure { get; private init; }

    [MemberNotNullWhen(true, nameof(Package))]
    [MemberNotNullWhen(false, nameof(Failure))]
    public bool IsResolved => Package is not null;

    public static PackageResolution Resolved(GalleryPackage package, IReadOnlyList<PackageVersion> versions) =>
        new() { Package = package, Versions = versions };

    public static PackageResolution Unresolved(PackageLookupFailure failure) =>
        new() { Failure = failure };
}
