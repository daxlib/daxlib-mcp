using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Packaging;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Infrastructure.Gallery;

/// <inheritdoc cref="IPackageProvider"/>
internal sealed partial class PackageProvider(
    IGalleryClient galleryClient,
    ILogger<PackageProvider> logger) : IPackageProvider
{
    private readonly IGalleryClient _galleryClient = galleryClient;
    private readonly ILogger<PackageProvider> _logger = logger;

    public async Task<Package?> GetPackageAsync(PackageId id, PackageVersion version, CancellationToken cancellationToken)
    {
        try
        {
            using var download = await _galleryClient.DownloadPackageAsync(id, version, cancellationToken);

            if (download is null)
            {
                LogPackageAcquisitionCompleted(id, version, "NotFound", downloadedBytes: 0);
                return null;
            }

            // Package.Open requires a seekable stream, so buffer the download into memory.
            // The largest published package is under a quarter of a megabyte.

            var buffer = new MemoryStream();
            await download.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            var package = Package.Open(buffer);

            LogPackageAcquisitionCompleted(id, version, "Success", buffer.Length);
            return package;
        }
        catch (OperationCanceledException)
        {
            LogPackageAcquisitionCompleted(id, version, "Cancelled", downloadedBytes: 0);
            throw;
        }
        catch (Exception exception)
        {
            var errorType = exception.GetType().Name;
            LogPackageAcquisitionFailed(id, version, errorType, exception);
            throw;
        }
    }
}
