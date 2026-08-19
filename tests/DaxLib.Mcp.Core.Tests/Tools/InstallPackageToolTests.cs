using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.AnalysisServices;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.SdkModeling;
using DaxLib.Mcp.Core.Tests.Logging;
using DaxLib.Mcp.Core.Tools;
using DaxLib.Packaging;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.Tools;

public sealed class InstallPackageToolTests
{
    private const string DataSource = "localhost:51542";

    private readonly IGalleryClient _galleryClient = Substitute.For<IGalleryClient>();
    private readonly IPackageProvider _packageProvider = Substitute.For<IPackageProvider>();
    private readonly IPackageInstaller _installer = Substitute.For<IPackageInstaller>();
    private readonly IAnalysisServicesInstanceLocator _instanceLocator = Substitute.For<IAnalysisServicesInstanceLocator>();

    private InstallPackageTool CreateTool(ILogger<InstallPackageTool>? logger = null) => new(
        new PackageResolver(_galleryClient, NullLogger<PackageResolver>.Instance),
        _packageProvider,
        _installer,
        _instanceLocator,
        new ToolCallInvocation(NullLogger<ToolCallInvocation>.Instance),
        logger ?? NullLogger<InstallPackageTool>.Instance);

    public InstallPackageToolTests()
    {
        var instance = new AnalysisServicesInstance(
            1234, 51542, new DateTime(2026, 8, 23, 10, 0, 0), "PBIDesktop", "Sales - Power BI Desktop");

        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>())
            .Returns(TestData.Versions("2.0.0"));
        _galleryClient.GetPackageAsync(Arg.Any<PackageId>(), Arg.Any<PackageVersion>(), Arg.Any<CancellationToken>())
            .Returns(TestData.Package());
        _packageProvider.GetPackageAsync(Arg.Any<PackageId>(), Arg.Any<PackageVersion>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Package.Open(
                PackageBuilder.Create("DaxLib.SVG", "2.0.0")
                    .WithDescription("test")
                    .AddDocument(LibraryDocument.Create(PackagingConstants.TmdlFunctionsFilePath, "function 'DaxLib.SVG.Fn' = () => 1\n"))
                    .Build()));

        _instanceLocator.FindInstance(DataSource, Arg.Any<CancellationToken>()).Returns(instance);
    }

    [Fact]
    public async Task Install_NamedDataSource_InstallsAndReportsWhatWasWritten()
    {
        _installer.Install(Arg.Any<Package>(), Arg.Any<ConnectionInfo>())
            .Returns(new PackageInstallOutcome.Installed("Sales", 4));

        var result = await CreateTool().InstallPackageAsync(
            "DaxLib.SVG", DataSource, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(true, result.IsError);
        Assert.Contains("4 functions", Text(result), StringComparison.Ordinal);
        _ = _installer.Received(1).Install(
            Arg.Any<Package>(),
            Arg.Is<ConnectionInfo>(connection =>
                connection.DataSource == DataSource
                && connection.ApplicationName == McpServerInfo.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("powerbi://api.powerbi.com/v1.0/myorg/Sales")]
    [InlineData("localhost:12345")]
    [InlineData("Data Source=localhost:51542;Password=hunter2")]
    public async Task Install_DataSourceThatIsNotRunning_IsRefusedWithoutDownloading(string dataSource)
    {
        var result = await CreateTool().InstallPackageAsync(
            "DaxLib.SVG", dataSource, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.False(Success(result));
        Assert.DoesNotContain("hunter2", Text(result), StringComparison.Ordinal);
        await _packageProvider.DidNotReceiveWithAnyArgs().GetPackageAsync(default!, default!, default!);
    }

    [Fact]
    public async Task Install_UnknownPackage_FailsBeforeInstalling()
    {
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>()).Returns([]);
        _galleryClient.GetPackageAsync(Arg.Any<PackageId>(), Arg.Any<PackageVersion>(), Arg.Any<CancellationToken>())
            .Returns((GalleryPackage?)null);
        _galleryClient.SearchPackagesAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateTool().InstallPackageAsync(
            "DaxLib.DoesNotExist", DataSource, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        _ = _installer.DidNotReceiveWithAnyArgs().Install(default!, default!);
    }

    /// <summary>
    /// An unexpected exception becomes a failure result that carries the exception message,
    /// instead of the SDK's generic invocation error.
    /// </summary>
    [Fact]
    public async Task Install_UnpredictedException_ComesBackAsAFailure()
    {
        _installer.Install(Arg.Any<Package>(), Arg.Any<ConnectionInfo>())
            .Throws(new InvalidTimeZoneException("nobody saw this coming"));

        var result = await CreateTool().InstallPackageAsync(
            "DaxLib.SVG", DataSource, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.False(Success(result));
        Assert.Contains("nobody saw this coming", Text(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Install_TypedInstallerFailure_LogsOutcomeAndException()
    {
        var exception = new InvalidOperationException("invalid package content");
        _installer.Install(Arg.Any<Package>(), Arg.Any<ConnectionInfo>())
            .Returns(new PackageInstallOutcome.PackageDeserializationFailed(exception));
        var logger = new RecordingLogger<InstallPackageTool>();

        var result = await CreateTool(logger).InstallPackageAsync(
            "DaxLib.SVG", DataSource, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Equal("PackageInstallFailed", warning.EventId.Name);
        Assert.Contains("PackageDeserializationFailed", warning.Message, StringComparison.Ordinal);
        Assert.Same(exception, warning.Exception);
    }

    private static string Text(CallToolResult result) =>
        string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    private static bool Success(CallToolResult result)
    {
        using var document = JsonDocument.Parse(Text(result));

        return document.RootElement.GetProperty("success").GetBoolean();
    }
}
