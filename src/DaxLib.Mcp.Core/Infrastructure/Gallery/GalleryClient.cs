using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Client;
using DaxLib.Client.Models;
using DaxLib.Packaging;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Infrastructure.Gallery;

/// <inheritdoc cref="IGalleryClient"/>
internal sealed partial class GalleryClient(
    DaxLibClient client,
    ILogger<GalleryClient> logger) : IGalleryClient
{
    private readonly DaxLibClient _client = client;
    private readonly ILogger<GalleryClient> _logger = logger;

    public async Task<IReadOnlyList<GalleryPackage>> SearchPackagesAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        LogSearchStarted(request.Query, request.Skip, request.Limit, request.IncludePrerelease);

        var results = await _client.SearchAsync(
            request.Query, request.Skip, request.Limit, request.IncludePrerelease, cancellationToken);

        LogSearchCompleted(request.Query, results.Count);

        return [.. results.Select(ToGalleryPackage)];
    }

    public async Task<IReadOnlyList<PackageVersion>> GetVersionsAsync(PackageId id, CancellationToken cancellationToken)
    {
        var versions = await _client.GetPackageVersionsAsync(id, cancellationToken);

        // The gallery documents a descending order but the SDK does not guarantee or restore it.
        // PackageVersion carries NuGet SemVer 2.0 comparison, so order it here.
        var ordered = versions.OrderByDescending(version => version).ToArray();

        LogPackageVersionsCompleted(id, ordered.Length);
        return ordered;
    }

    public async Task<GalleryPackage?> GetPackageAsync(PackageId id, PackageVersion version, CancellationToken cancellationToken)
    {
        var metadata = await _client.GetPackageMetadataAsync(id, version, cancellationToken);

        LogPackageMetadataCompleted(id, version, metadata is not null);

        return metadata is null ? null : ToGalleryPackage(metadata);
    }

    public async Task<Stream?> DownloadPackageAsync(PackageId id, PackageVersion version, CancellationToken cancellationToken)
    {
        LogPackageDownloadStarted(id, version);

        return await _client.DownloadPackageAsync(id, version, cancellationToken);
    }

    public async Task<string?> GetReadmeAsync(PackageId id, PackageVersion version, CancellationToken cancellationToken)
    {
        using var stream = await _client.DownloadPackageReadmeAsync(id, version, cancellationToken);

        if (stream is null)
        {
            LogPackageReadmeCompleted(id, version, found: false, characterCount: 0);
            return null;
        }

        using var reader = new StreamReader(stream);
        var readme = await reader.ReadToEndAsync(cancellationToken);

        LogPackageReadmeCompleted(id, version, found: true, readme.Length);
        return readme;
    }

    private static GalleryPackage ToGalleryPackage(PackageMetadata metadata) => new()
    {
        Id = metadata.PackageId,
        Version = metadata.Version,
        Description = metadata.Description,
        Authors = metadata.Authors,
        Owners = metadata.Owners,
        Tags = metadata.Tags,
        ReleaseNotes = metadata.ReleaseNotes,
        ProjectUrl = metadata.ProjectUrl,
        RepositoryUrl = metadata.RepositoryUrl,
        Published = metadata.Published,
        Downloads = metadata.Downloads,
        HasReadme = metadata.HasReadme,
    };
}
