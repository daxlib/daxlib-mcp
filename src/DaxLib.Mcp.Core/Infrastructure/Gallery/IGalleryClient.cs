using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.Infrastructure.Gallery;

/// <summary>
/// A client for the DAX Lib gallery API.
/// </summary>
internal interface IGalleryClient
{
    /// <summary>
    /// Searches the DAX Lib gallery for packages matching the request, returning a page of results.
    /// </summary>
    Task<IReadOnlyList<GalleryPackage>> SearchPackagesAsync(SearchRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Returns every version of a package, newest first, or an empty collection when the id is unknown.
    /// </summary>
    Task<IReadOnlyList<PackageVersion>> GetVersionsAsync(PackageId id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the package metadata, or <see langword="null"/> when the id or version is unknown.
    /// </summary>
    Task<GalleryPackage?> GetPackageAsync(PackageId id, PackageVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a package as a <see cref="Stream"/>, or <see langword="null"/> when the id or version is unknown.
    /// </summary>
    Task<Stream?> DownloadPackageAsync(PackageId id, PackageVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the package README as Markdown, or <see langword="null"/> when the package has none.
    /// </summary>
    Task<string?> GetReadmeAsync(PackageId id, PackageVersion version, CancellationToken cancellationToken);
}
