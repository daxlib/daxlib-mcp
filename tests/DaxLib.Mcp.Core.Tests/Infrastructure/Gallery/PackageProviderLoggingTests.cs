using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.Tests.Logging;
using DaxLib.Packaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.Infrastructure.Gallery;

public sealed class PackageProviderLoggingTests
{
    private readonly IGalleryClient _galleryClient = Substitute.For<IGalleryClient>();
    private readonly RecordingLogger<PackageProvider> _logger = new();

    [Fact]
    public async Task GetPackage_DownloadedPackage_LogsSuccessAndByteCount()
    {
        _galleryClient.DownloadPackageAsync(
                Arg.Any<PackageId>(),
                Arg.Any<PackageVersion>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => BuildPackage());

        using var package = await CreateProvider().GetPackageAsync(
            TestData.Id("DaxLib.SVG"),
            TestData.Version("2.0.0"),
            TestContext.Current.CancellationToken);

        Assert.NotNull(package);
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("PackageAcquisitionCompleted", entry.EventId.Name);
        Assert.Contains("Outcome: Success", entry.Message, System.StringComparison.Ordinal);
        Assert.DoesNotContain("downloaded bytes: 0", entry.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPackage_MissingDownload_LogsNotFound()
    {
        _galleryClient.DownloadPackageAsync(
                Arg.Any<PackageId>(),
                Arg.Any<PackageVersion>(),
                Arg.Any<CancellationToken>())
            .Returns((Stream?)null);

        var package = await CreateProvider().GetPackageAsync(
            TestData.Id("DaxLib.Missing"),
            TestData.Version("1.0.0"),
            TestContext.Current.CancellationToken);

        Assert.Null(package);
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("Outcome: NotFound", entry.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetPackage_DownloadFailure_LogsSingleWarningWithException()
    {
        var exception = new System.InvalidOperationException("download failed");
        _galleryClient.DownloadPackageAsync(
                Arg.Any<PackageId>(),
                Arg.Any<PackageVersion>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Stream?>(exception));

        _ = await Assert.ThrowsAsync<System.InvalidOperationException>(() => CreateProvider().GetPackageAsync(
            TestData.Id("DaxLib.SVG"),
            TestData.Version("2.0.0"),
            TestContext.Current.CancellationToken));

        var warning = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Equal("PackageAcquisitionFailed", warning.EventId.Name);
        Assert.Same(exception, warning.Exception);
    }

    private PackageProvider CreateProvider() => new(_galleryClient, _logger);

    private static Stream BuildPackage() =>
        PackageBuilder.Create("DaxLib.SVG", "2.0.0")
            .WithDescription("test")
            .AddDocument(LibraryDocument.Create(PackagingConstants.TmdlFunctionsFilePath, "function 'DaxLib.SVG.Fn' = () => 1\n"))
            .Build();
}
