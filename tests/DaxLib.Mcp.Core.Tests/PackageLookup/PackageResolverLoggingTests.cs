using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.Tests.Logging;
using DaxLib.Packaging;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.PackageLookup;

public sealed class PackageResolverLoggingTests
{
    private readonly IGalleryClient _galleryClient = Substitute.For<IGalleryClient>();
    private readonly RecordingLogger<PackageResolver> _logger = new();

    [Fact]
    public async Task Resolve_KnownPackage_LogsResolvedVersionAndCount()
    {
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>())
            .Returns(TestData.Versions("2.0.0", "1.0.0"));
        _galleryClient.GetPackageAsync(Arg.Any<PackageId>(), Arg.Any<PackageVersion>(), Arg.Any<CancellationToken>())
            .Returns(TestData.Package());

        var resolution = await CreateResolver().ResolveAsync(
            "DaxLib.SVG",
            version: null,
            TestContext.Current.CancellationToken);

        Assert.True(resolution.IsResolved);
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("PackageResolutionCompleted", entry.EventId.Name);
        Assert.Contains("2.0.0", entry.Message, System.StringComparison.Ordinal);
        Assert.Contains("available versions: 2", entry.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public async Task Resolve_UnknownPackage_LogsExpectedOutcomeWithoutWarning()
    {
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>()).Returns([]);
        _galleryClient.SearchPackagesAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>()).Returns([]);

        var resolution = await CreateResolver().ResolveAsync(
            "DaxLib.DoesNotExist",
            version: null,
            TestContext.Current.CancellationToken);

        Assert.False(resolution.IsResolved);
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("PackageResolutionUnknownPackage", entry.EventId.Name);
    }

    private PackageResolver CreateResolver() => new(_galleryClient, _logger);
}
