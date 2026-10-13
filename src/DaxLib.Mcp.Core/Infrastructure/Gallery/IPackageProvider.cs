using System.Threading;
using System.Threading.Tasks;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.Infrastructure.Gallery;

/// <summary>
/// Provides access to published <c>.daxpkg</c> packages from the DAX Lib gallery.
/// </summary>
internal interface IPackageProvider
{
    /// <summary>
    /// Downloads and opens a package, or returns <see langword="null"/> when the id or version is
    /// unknown.
    /// </summary>
    Task<Package?> GetPackageAsync(PackageId id, PackageVersion version, CancellationToken cancellationToken);
}
