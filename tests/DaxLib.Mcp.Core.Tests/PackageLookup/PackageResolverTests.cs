using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.Tests.Logging;
using DaxLib.Packaging;
using NSubstitute;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.PackageLookup;

public sealed class PackageResolverTests
{
    private readonly IGalleryClient _galleryClient = Substitute.For<IGalleryClient>();
    private readonly RecordingLogger<PackageResolver> _logger = new();

    [Fact]
    public async Task Resolve_EmptyId_ReturnsUnknownPackage()
    {
        var resolution = await CreateResolver().ResolveAsync(
            string.Empty,
            version: null,
            TestContext.Current.CancellationToken);

        var failure = Assert.IsType<PackageLookupFailure.UnknownPackage>(resolution.Failure);
        Assert.Empty(failure.Suggestions);
    }

    [Fact]
    public async Task Resolve_MalformedId_ReturnsGallerySuggestions()
    {
        _galleryClient.SearchPackagesAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([TestData.Package()]);

        var resolution = await CreateResolver().ResolveAsync(
            "invalid package",
            version: null,
            TestContext.Current.CancellationToken);

        var failure = Assert.IsType<PackageLookupFailure.UnknownPackage>(resolution.Failure);
        Assert.Equal([TestData.Id("DaxLib.SVG")], failure.Suggestions);
    }

    [Fact]
    public async Task Resolve_WithoutVersion_SelectsLatestListedVersion()
    {
        var latest = TestData.Version("2.0.0");
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>())
            .Returns([latest, TestData.Version("1.0.0")]);
        _galleryClient.GetPackageAsync(
                Arg.Any<PackageId>(),
                Arg.Is<PackageVersion>(candidate => candidate == latest),
                Arg.Any<CancellationToken>())
            .Returns(TestData.Package(version: "2.0.0"));

        var resolution = await CreateResolver().ResolveAsync(
            "DaxLib.SVG",
            version: null,
            TestContext.Current.CancellationToken);

        Assert.True(resolution.IsResolved);
        Assert.Equal(latest, resolution.Package.Version);
    }

    [Fact]
    public async Task Resolve_MalformedVersion_ReturnsUnknownVersion()
    {
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>())
            .Returns(TestData.Versions("1.0.0"));

        var resolution = await CreateResolver().ResolveAsync(
            "DaxLib.SVG",
            "not-a-version",
            TestContext.Current.CancellationToken);

        var failure = Assert.IsType<PackageLookupFailure.UnknownVersion>(resolution.Failure);
        Assert.Equal("not-a-version", failure.RequestedVersion);
    }

    [Fact]
    public async Task Resolve_UnlistedExplicitVersion_ResolvesWhenAddressable()
    {
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>())
            .Returns(TestData.Versions("1.0.0"));
        _galleryClient.GetPackageAsync(
                Arg.Any<PackageId>(),
                Arg.Is<PackageVersion>(candidate => candidate == TestData.Version("2.0.0")),
                Arg.Any<CancellationToken>())
            .Returns(TestData.Package(version: "2.0.0"));

        var resolution = await CreateResolver().ResolveAsync(
            "DaxLib.SVG",
            "2.0.0",
            TestContext.Current.CancellationToken);

        Assert.True(resolution.IsResolved);
        Assert.Equal(TestData.Version("2.0.0"), resolution.Package.Version);
    }

    [Fact]
    public async Task Resolve_MissingExplicitVersion_ReturnsUnknownVersionForKnownPackage()
    {
        var versions = TestData.Versions("1.0.0");
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>())
            .Returns(versions);
        _galleryClient.GetPackageAsync(
                Arg.Any<PackageId>(),
                Arg.Any<PackageVersion>(),
                Arg.Any<CancellationToken>())
            .Returns((GalleryPackage?)null);

        var resolution = await CreateResolver().ResolveAsync(
            "DaxLib.SVG",
            "2.0.0",
            TestContext.Current.CancellationToken);

        var failure = Assert.IsType<PackageLookupFailure.UnknownVersion>(resolution.Failure);
        Assert.Equal("2.0.0", failure.RequestedVersion);
        Assert.Same(versions, failure.AvailableVersions);
    }

    [Fact]
    public async Task Resolve_MissingExplicitVersion_ReturnsUnknownPackageWhenNoVersionsExist()
    {
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _galleryClient.GetPackageAsync(
                Arg.Any<PackageId>(),
                Arg.Any<PackageVersion>(),
                Arg.Any<CancellationToken>())
            .Returns((GalleryPackage?)null);
        _galleryClient.SearchPackagesAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var resolution = await CreateResolver().ResolveAsync(
            "DaxLib.Missing",
            "2.0.0",
            TestContext.Current.CancellationToken);

        _ = Assert.IsType<PackageLookupFailure.UnknownPackage>(resolution.Failure);
    }

    private PackageResolver CreateResolver() => new(_galleryClient, _logger);
}
