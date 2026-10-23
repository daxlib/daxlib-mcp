using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.QueryDefinitions;
using DaxLib.Mcp.Core.Tests.Fixtures;
using DaxLib.Mcp.Core.Tools;
using DaxLib.Packaging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Protocol;
using NSubstitute;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.Tools;

public sealed class GetFunctionDefinitionsToolTests
{
    private readonly IGalleryClient _galleryClient = Substitute.For<IGalleryClient>();
    private readonly IPackageProvider _packageProvider = Substitute.For<IPackageProvider>();

    private GetFunctionDefinitionsTool CreateTool() => new(
        new PackageResolver(_galleryClient, NullLogger<PackageResolver>.Instance),
        _packageProvider,
        new DependencyResolver(_galleryClient, _packageProvider),
        new ToolCallInvocation(NullLogger<ToolCallInvocation>.Instance),
        NullLogger<GetFunctionDefinitionsTool>.Instance);

    [Fact]
    public async Task Get_Sample_ReturnsTheEnvelopeAndTheDefineBlockAsAResource()
    {
        Publish("DaxLib.Sample", "0.1.8");

        var result = await CreateTool().GetFunctionDefinitionsAsync(
            "daxlib.sample", cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(true, result.IsError);

        using var envelope = JsonDocument.Parse(Text(result));
        var root = envelope.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.Equal(
            "DaxLib.Sample 0.1.8: 3 functions from 1 package. 0 warnings. The DEFINE block is attached.",
            root.GetProperty("message").GetString());

        var data = root.GetProperty("data");
        Assert.Equal("DaxLib.Sample", data.GetProperty("id").GetString());
        Assert.Equal("0.1.8", data.GetProperty("version").GetString());
        Assert.Equal("all", data.GetProperty("includeDependencies").GetString());
        Assert.True(data.GetProperty("complete").GetBoolean());
        Assert.Equal(0, data.GetProperty("warnings").GetArrayLength());

        var package = Assert.Single(data.GetProperty("packages").EnumerateArray());
        Assert.Equal("requested", package.GetProperty("role").GetString());
        Assert.Equal(3, package.GetProperty("functions").GetArrayLength());

        var myFunc3 = data.GetProperty("functions")[2];
        Assert.Equal("DaxLib.Sample.MyFunc3", myFunc3.GetProperty("name").GetString());
        Assert.Equal("MyFunc3 description", myFunc3.GetProperty("description").GetString());
        Assert.Equal("requested", myFunc3.GetProperty("reason").GetString());
        var parameter = myFunc3.GetProperty("parameters")[0];
        Assert.Equal(("param1", "INT64", false, "Parameter description"), (
            parameter.GetProperty("name").GetString(),
            parameter.GetProperty("type").GetString(),
            parameter.GetProperty("optional").GetBoolean(),
            parameter.GetProperty("description").GetString()));

        var resource = Assert.IsType<TextResourceContents>(Assert.Single(result.Content.OfType<EmbeddedResourceBlock>()).Resource);
        Assert.Equal("daxlib-define://DaxLib.Sample/0.1.8/define.dax", resource.Uri);
        Assert.Equal("text/plain", resource.MimeType);
        Assert.StartsWith("DEFINE\n    // MyFunc1 description\n    FUNCTION DaxLib.Sample.MyFunc1 =", resource.Text, StringComparison.Ordinal);
        Assert.Equal(resource.Text.Length, data.GetProperty("characters").GetInt32());

        // The manifest declares DaxLib.Records, but no function calls into it.
        await _galleryClient.DidNotReceive().GetVersionsAsync(
            Arg.Is<PackageId>(id => id.ToNormalizedString() == "DaxLib.Records"), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Acceptance case T18/P09: the closure of two SVG visuals has 9 functions and is about 8 times
    /// smaller than the package.
    /// </summary>
    [Fact]
    public async Task Get_SvgVisuals_ReturnsTheirCallClosure()
    {
        Publish("DaxLib.SVG", "2.0.0");

        var result = await CreateTool().GetFunctionDefinitionsAsync(
            "DaxLib.SVG",
            functions: ["DaxLib.SVG.Viz.Pill", "daxlib.svg.viz.progressbar"],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(true, result.IsError);
        using var envelope = JsonDocument.Parse(Text(result));
        var data = envelope.RootElement.GetProperty("data");
        Assert.Equal(9, data.GetProperty("functions").GetArrayLength());
        Assert.Equal(
            ["dependency", "dependency", "dependency", "dependency", "dependency", "dependency", "dependency", "requested", "requested"],
            data.GetProperty("functions").EnumerateArray().Select(function => function.GetProperty("reason").GetString()));
        Assert.InRange(data.GetProperty("characters").GetInt32(), 16_000, 17_000);
    }

    [Fact]
    public async Task Get_UnknownFunction_FailsWithCloseNames()
    {
        Publish("DaxLib.SVG", "2.0.0");

        var result = await CreateTool().GetFunctionDefinitionsAsync(
            "DaxLib.SVG", functions: ["DaxLib.SVG.Viz.Pil"], cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        using var envelope = JsonDocument.Parse(Text(result));
        Assert.False(envelope.RootElement.GetProperty("success").GetBoolean());
        var data = envelope.RootElement.GetProperty("data");
        Assert.Equal("unknownFunction", data.GetProperty("code").GetString());
        Assert.Equal("DaxLib.SVG.Viz.Pil", data.GetProperty("functions")[0].GetString());
        Assert.Equal("DaxLib.SVG.Viz.Pill", data.GetProperty("suggestions")[0].GetString());
        Assert.InRange(data.GetProperty("suggestions").GetArrayLength(), 1, 10);
        Assert.Empty(result.Content.OfType<EmbeddedResourceBlock>());
    }

    [Fact]
    public async Task Get_EmptyFunctionList_FailsBeforeDownloading()
    {
        var result = await CreateTool().GetFunctionDefinitionsAsync(
            "DaxLib.SVG", functions: [], cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        await _packageProvider.DidNotReceiveWithAnyArgs().GetPackageAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Get_UnknownPackage_FailsWithTheLookupError()
    {
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>()).Returns([]);
        _galleryClient.SearchPackagesAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateTool().GetFunctionDefinitionsAsync(
            "DaxLib.DoesNotExist", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        using var envelope = JsonDocument.Parse(Text(result));
        Assert.StartsWith(
            "No DAX Lib package with id 'DaxLib.DoesNotExist' exists",
            envelope.RootElement.GetProperty("message").GetString(),
            StringComparison.Ordinal);
        await _packageProvider.DidNotReceiveWithAnyArgs().GetPackageAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Get_RecursiveFunctions_FailsWithTheCycle()
    {
        PublishTmdl("A.Pkg", "1.0.0", "function 'A.Pkg.F' = () => A.Pkg.G ()\n\nfunction 'A.Pkg.G' = () => A.Pkg.F ()\n");

        var result = await CreateTool().GetFunctionDefinitionsAsync(
            "A.Pkg", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        using var envelope = JsonDocument.Parse(Text(result));
        var data = envelope.RootElement.GetProperty("data");
        Assert.Equal("recursiveCall", data.GetProperty("code").GetString());
        Assert.Equal(["A.Pkg.F", "A.Pkg.G", "A.Pkg.F"], data.GetProperty("functions").EnumerateArray().Select(name => name.GetString()));
    }

    [Fact]
    public async Task Get_InvalidTmdl_FailsWithTmdlParseFailed()
    {
        PublishTmdl("A.Pkg", "1.0.0", "this is not tmdl\n");

        var result = await CreateTool().GetFunctionDefinitionsAsync(
            "A.Pkg", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        using var envelope = JsonDocument.Parse(Text(result));
        Assert.Equal("tmdlParseFailed", envelope.RootElement.GetProperty("data").GetProperty("code").GetString());
    }

    private void Publish(string packageId, string version)
    {
        SetUpGallery(packageId, version);
        _packageProvider.GetPackageAsync(Arg.Any<PackageId>(), Arg.Any<PackageVersion>(), Arg.Any<CancellationToken>())
            .Returns(_ => FixtureLoader.OpenPackage(packageId, version));
    }

    private void PublishTmdl(string packageId, string version, string tmdl)
    {
        SetUpGallery(packageId, version);
        _packageProvider.GetPackageAsync(Arg.Any<PackageId>(), Arg.Any<PackageVersion>(), Arg.Any<CancellationToken>())
            .Returns(_ => Package.Open(PackageBuilder.Create(packageId, version)
                .WithDescription("test")
                .AddDocument(LibraryDocument.Create(PackagingConstants.TmdlFunctionsFilePath, tmdl))
                .Build()));
    }

    private void SetUpGallery(string packageId, string version)
    {
        _galleryClient.GetVersionsAsync(
                Arg.Is<PackageId>(id => string.Equals(id.ToNormalizedString(), packageId, StringComparison.OrdinalIgnoreCase)),
                Arg.Any<CancellationToken>())
            .Returns(TestData.Versions(version));
        _galleryClient.GetPackageAsync(Arg.Any<PackageId>(), Arg.Any<PackageVersion>(), Arg.Any<CancellationToken>())
            .Returns(TestData.Package(packageId, version));
    }

    private static string Text(CallToolResult result) =>
        string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));
}
