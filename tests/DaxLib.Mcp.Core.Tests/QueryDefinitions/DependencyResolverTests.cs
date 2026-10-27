using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.QueryDefinitions;
using DaxLib.Packaging;
using NSubstitute;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.QueryDefinitions;

/// <summary>
/// The call closure over a fake gallery: packages are built in memory, so every dependency case
/// can be set up, including those that no published package has today.
/// </summary>
public sealed class DependencyResolverTests
{
    private static readonly string[] s_svgVisuals = ["DaxLib.SVG.Viz.Pill", "DaxLib.SVG.Viz.ProgressBar"];

    private readonly IGalleryClient _galleryClient = Substitute.For<IGalleryClient>();
    private readonly IPackageProvider _packageProvider = Substitute.For<IPackageProvider>();
    private readonly Dictionary<string, Dictionary<string, FakePackage>> _gallery = new(StringComparer.OrdinalIgnoreCase);

    public DependencyResolverTests()
    {
        _galleryClient.GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>())
            .Returns(call => _gallery.TryGetValue(call.Arg<PackageId>().ToNormalizedString(), out var versions)
                ? (IReadOnlyList<PackageVersion>)[.. versions.Keys.Select(TestData.Version)]
                : []);

        _packageProvider.GetPackageAsync(Arg.Any<PackageId>(), Arg.Any<PackageVersion>(), Arg.Any<CancellationToken>())
            .Returns(call => _gallery.TryGetValue(call.Arg<PackageId>().ToNormalizedString(), out var versions)
                && versions.TryGetValue(call.Arg<PackageVersion>().ToNormalizedString(), out var package)
                    ? package.Open()
                    : null);
    }

    [Fact]
    public async Task Resolve_ModeNone_ReturnsOnlyTheSeedsAndReportsEveryMissingCallee()
    {
        var requested = SetUpCallerWithLocalAndExternalCalls();

        var closure = await ResolveAsync(requested, IncludeDependencies.None, "A.Pkg.F");

        Assert.Equal(["A.Pkg.F"], Names(closure));
        Assert.False(closure.IsComplete);
        Assert.Equal(
            [("missingDependency", "A.Pkg.G"), ("missingDependency", "B.Pkg.X")],
            closure.Warnings.Select(warning => (warning.Code, warning.Function!)).Order());
        Assert.All(closure.Warnings, warning => Assert.Equal(["A.Pkg.F"], warning.Callers));
        await AssertNoDownloadAsync();
    }

    [Fact]
    public async Task Resolve_ModeNone_CalleeThatIsAlsoRequested_IsNotMissing()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [], ("A.Pkg.F", "() => A.Pkg.G ()"), ("A.Pkg.G", "() => 1"));

        var closure = await ResolveAsync(requested, IncludeDependencies.None, "A.Pkg.F", "A.Pkg.G");

        Assert.Equal(["A.Pkg.F", "A.Pkg.G"], Names(closure));
        Assert.True(closure.IsComplete);
        Assert.Empty(closure.Warnings);
    }

    [Fact]
    public async Task Resolve_ModePackage_AddsLocalCalleesOnly()
    {
        var requested = SetUpCallerWithLocalAndExternalCalls();

        var closure = await ResolveAsync(requested, IncludeDependencies.Package, "A.Pkg.F");

        Assert.Equal(["A.Pkg.F", "A.Pkg.G"], Names(closure));
        var warning = Assert.Single(closure.Warnings);
        Assert.Equal(("missingDependency", "B.Pkg", "1.0.0", "B.Pkg.X"), (warning.Code, warning.Package, warning.Version, warning.Function));
        Assert.False(closure.IsComplete);
        await AssertNoDownloadAsync();
    }

    [Fact]
    public async Task Resolve_ModeAll_AddsCalleesOfTheDeclaredDependencyAtTheExactVersion()
    {
        var requested = SetUpCallerWithLocalAndExternalCalls();

        var closure = await ResolveAsync(requested, IncludeDependencies.All, "A.Pkg.F");

        Assert.True(closure.IsComplete);
        Assert.Empty(closure.Warnings);
        Assert.Equal(["B.Pkg.X", "A.Pkg.F", "A.Pkg.G"], Names(closure));
        Assert.Equal(
            [("B.Pkg", "1.0.0", false), ("A.Pkg", "1.0.0", true)],
            closure.Packages.Select(package => (package.Catalog.PackageId, package.Catalog.Version.ToNormalizedString(), package.IsRequested)));
        Assert.Equal(["A.Pkg.G", "B.Pkg.X"], closure.Functions.Single(function => function.Function.Name == "A.Pkg.F").Calls);
        Assert.Equal([false, true, false], closure.Functions.Select(function => function.IsRequested));
        await _packageProvider.Received(1).GetPackageAsync(
            Arg.Is<PackageId>(id => id.ToNormalizedString() == "B.Pkg"),
            Arg.Is<PackageVersion>(version => version.ToNormalizedString() == "1.0.0"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_DeclaredVersionNotPublished_WarnsWithThePublishedVersions()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [("B.Pkg", "1.0.0")], ("A.Pkg.F", "() => B.Pkg.X ()"), ("A.Pkg.H", "() => B.Pkg.X ()"));
        AddPackage("B.Pkg", "2.0.0", [], ("B.Pkg.X", "() => 1"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All);

        Assert.False(closure.IsComplete);
        Assert.Equal(["A.Pkg.F", "A.Pkg.H"], Names(closure));
        var warning = Assert.Single(closure.Warnings);
        Assert.Equal("dependencyVersionNotFound", warning.Code);
        Assert.Equal(("B.Pkg", "1.0.0"), (warning.Package, warning.Version));
        Assert.Equal(["2.0.0"], warning.PublishedVersions);
        Assert.Equal(["A.Pkg.F", "A.Pkg.H"], warning.Callers);
        await _packageProvider.DidNotReceiveWithAnyArgs().GetPackageAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Resolve_DependencyNotPublished_WarnsDependencyNotFound()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [("B.Pkg", "1.0.0")], ("A.Pkg.F", "() => B.Pkg.X ()"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All);

        Assert.False(closure.IsComplete);
        var warning = Assert.Single(closure.Warnings);
        Assert.Equal(("dependencyNotFound", "B.Pkg"), (warning.Code, warning.Package));
        Assert.Equal(["A.Pkg.F"], warning.Callers);
    }

    [Fact]
    public async Task Resolve_CalleeNotInTheDeclaredVersion_WarnsFunctionNotFound()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [("B.Pkg", "1.0.0")], ("A.Pkg.F", "() => B.Pkg.Missing ()"));
        AddPackage("B.Pkg", "1.0.0", [], ("B.Pkg.X", "() => 1"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All);

        Assert.False(closure.IsComplete);
        var warning = Assert.Single(closure.Warnings);
        Assert.Equal(("functionNotFound", "B.Pkg.Missing", "1.0.0"), (warning.Code, warning.Function, warning.Version));
        Assert.Equal(["A.Pkg.F"], Names(closure));
    }

    /// <summary>
    /// A needs B 1.0.0 and C; C needs B 2.0.0. The first version loaded stays.
    /// </summary>
    [Fact]
    public async Task Resolve_Diamond_KeepsTheFirstVersionAndWarnsVersionConflict()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [("B.Pkg", "1.0.0"), ("C.Pkg", "1.0.0")], ("A.Pkg.F", "() => B.Pkg.X () + C.Pkg.Y ()"));
        AddPackage("B.Pkg", "1.0.0", [], ("B.Pkg.X", "() => 1"));
        AddPackage("B.Pkg", "2.0.0", [], ("B.Pkg.X", "() => 2"));
        AddPackage("C.Pkg", "1.0.0", [("B.Pkg", "2.0.0")], ("C.Pkg.Y", "() => B.Pkg.X ()"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All);

        Assert.False(closure.IsComplete);
        Assert.Contains(closure.Packages, package => package.Catalog.PackageId == "B.Pkg" && package.Catalog.Version.ToNormalizedString() == "1.0.0");
        var warning = Assert.Single(closure.Warnings);
        Assert.Equal(("versionConflict", "B.Pkg"), (warning.Code, warning.Package));
        Assert.Equal(["1.0.0", "2.0.0"], warning.Versions);
        Assert.Equal(["C.Pkg.Y"], warning.Callers);
    }

    [Fact]
    public async Task Resolve_DependencyNeedsAnotherVersionOfTheRequestedPackage_RequestedVersionStays()
    {
        var requested = AddPackage("A.Pkg", "2.0.0", [("B.Pkg", "1.0.0")], ("A.Pkg.F", "() => B.Pkg.X ()"), ("A.Pkg.G", "() => 1"));
        AddPackage("B.Pkg", "1.0.0", [("A.Pkg", "1.0.0")], ("B.Pkg.X", "() => A.Pkg.G ()"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All, "A.Pkg.F");

        var warning = Assert.Single(closure.Warnings, warning => warning.Code == "versionConflict");
        Assert.Equal("A.Pkg", warning.Package);
        Assert.Equal(["2.0.0", "1.0.0"], warning.Versions);
        Assert.Equal(["B.Pkg.X", "A.Pkg.F"], Names(closure));
    }

    [Fact]
    public async Task Resolve_PackageCycle_StopsAndWarnsAsInformation()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [("B.Pkg", "1.0.0")], ("A.Pkg.F", "() => B.Pkg.X ()"), ("A.Pkg.G", "() => 1"));
        AddPackage("B.Pkg", "1.0.0", [("A.Pkg", "1.0.0")], ("B.Pkg.X", "() => A.Pkg.G ()"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All, "A.Pkg.F");

        Assert.True(closure.IsComplete);
        Assert.Equal(["B.Pkg.X", "A.Pkg.F", "A.Pkg.G"], Names(closure));
        var warning = Assert.Single(closure.Warnings);
        Assert.Equal("packageCycle", warning.Code);
        Assert.Contains("A.Pkg → B.Pkg → A.Pkg", warning.Message, StringComparison.Ordinal);
        await _galleryClient.Received(1).GetVersionsAsync(Arg.Any<PackageId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolve_CallIntoALoadedPackageThatIsNotDeclared_WarnsAndDoesNotAddIt()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [("B.Pkg", "1.0.0"), ("C.Pkg", "1.0.0")], ("A.Pkg.F", "() => B.Pkg.X () + C.Pkg.Y ()"));
        AddPackage("B.Pkg", "1.0.0", [], ("B.Pkg.X", "() => C.Pkg.Z ()"));
        AddPackage("C.Pkg", "1.0.0", [], ("C.Pkg.Y", "() => 1"), ("C.Pkg.Z", "() => 2"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All);

        Assert.True(closure.IsComplete);
        Assert.DoesNotContain("C.Pkg.Z", Names(closure));
        var warning = Assert.Single(closure.Warnings);
        Assert.Equal(("undeclaredDependency", "C.Pkg", "C.Pkg.Z"), (warning.Code, warning.Package, warning.Function));
        Assert.Equal(["B.Pkg.X"], warning.Callers);
    }

    [Fact]
    public async Task Resolve_UnknownDottedCall_IsInformationOnly()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [], ("A.Pkg.F", "() => Model.Fn ( 1 ) + NORM.S.DIST ( 1, TRUE ) + Keyword ( 1 )"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All);

        Assert.True(closure.IsComplete);
        var warning = Assert.Single(closure.Warnings);
        Assert.Equal(("unresolvedCall", "Model.Fn"), (warning.Code, warning.Function));
        Assert.Equal(["Model.Fn", "Keyword"], closure.Functions.Single().Calls);
    }

    [Fact]
    public async Task Resolve_NoCallLeavesThePackage_DownloadsNothing()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [("B.Pkg", "1.0.0")], ("A.Pkg.F", "() => A.Pkg.G ()"), ("A.Pkg.G", "() => 1"));
        AddPackage("B.Pkg", "1.0.0", [], ("B.Pkg.X", "() => 1"));

        var closure = await ResolveAsync(requested, IncludeDependencies.All, "A.Pkg.F");

        Assert.Equal(["A.Pkg.F", "A.Pkg.G"], Names(closure));
        await AssertNoDownloadAsync();
    }

    [Fact]
    public async Task Resolve_UnrelatedFunctionsOfTheRequestedPackage_AreNotIncluded()
    {
        using var package = Fixtures.FixtureLoader.OpenPackage("DaxLib.SVG", "2.0.0");
        var catalog = FunctionCatalog.Create(package);
        CatalogFunction[] seeds = [.. s_svgVisuals.Select(name => catalog.Functions.Single(function => function.Name == name))];

        var closure = await CreateResolver().ResolveAsync(catalog, seeds, IncludeDependencies.All, TestContext.Current.CancellationToken);

        // The 9 functions of the T18/P09 query, in catalog order.
        Assert.True(closure.IsComplete);
        Assert.Equal(
            [
                "DaxLib.SVG.Attr.Shapes", "DaxLib.SVG.Attr.Txt", "DaxLib.SVG.Compound.Pill", "DaxLib.SVG.Compound.ProgressBar", "DaxLib.SVG.Element.Rect",
                "DaxLib.SVG.Element.Txt", "DaxLib.SVG.SVG", "DaxLib.SVG.Viz.Pill", "DaxLib.SVG.Viz.ProgressBar",
            ],
            Names(closure).Order(StringComparer.Ordinal));
        Assert.Equal(
            Names(closure),
            catalog.Functions.Select(function => function.Name).Where(Names(closure).Contains));
    }

    [Fact]
    public async Task Resolve_SelfCall_IsARecursionError()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [], ("A.Pkg.Fact", "( n : INT64 ) => IF ( n <= 1, 1, n * A.Pkg.Fact ( n - 1 ) )"));

        var closure = await ResolveAsync(requested, IncludeDependencies.None);

        var error = Assert.IsType<ClosureError.RecursiveCall>(closure.Error);
        Assert.Equal(["A.Pkg.Fact", "A.Pkg.Fact"], error.Cycle);
    }

    [Fact]
    public async Task Resolve_MutualRecursion_IsARecursionError()
    {
        var requested = AddPackage("A.Pkg", "1.0.0", [], ("A.Pkg.F", "() => A.Pkg.G ()"), ("A.Pkg.G", "() => A.Pkg.F ()"));

        var closure = await ResolveAsync(requested, IncludeDependencies.Package, "A.Pkg.F");

        var error = Assert.IsType<ClosureError.RecursiveCall>(closure.Error);
        Assert.Equal(["A.Pkg.F", "A.Pkg.G", "A.Pkg.F"], error.Cycle);
    }

    private FunctionCatalog SetUpCallerWithLocalAndExternalCalls()
    {
        var requested = AddPackage(
            "A.Pkg", "1.0.0", [("B.Pkg", "1.0.0")],
            ("A.Pkg.F", "() => A.Pkg.G () & B.Pkg.X () & \"A.Pkg.Unused ()\""),
            ("A.Pkg.G", "() => 1"),
            ("A.Pkg.Unused", "() => 0"));
        AddPackage("B.Pkg", "1.0.0", [], ("B.Pkg.X", "() => 1"), ("B.Pkg.Unused", "() => 0"));

        return requested;
    }

    private async Task<FunctionClosure> ResolveAsync(FunctionCatalog requested, IncludeDependencies mode, params string[] seeds)
    {
        var seedFunctions = seeds.Length == 0
            ? requested.Functions
            : [.. seeds.Select(name => requested.Functions.Single(function => function.Name == name))];

        var closure = await CreateResolver().ResolveAsync(requested, seedFunctions, mode, TestContext.Current.CancellationToken);

        if (closure.Error is { } error && error is not ClosureError.RecursiveCall)
            Assert.Fail($"Unexpected error: {error}");

        return closure;
    }

    private DependencyResolver CreateResolver() => new(_galleryClient, _packageProvider);

    private async Task AssertNoDownloadAsync()
    {
        await _galleryClient.DidNotReceiveWithAnyArgs().GetVersionsAsync(default!, TestContext.Current.CancellationToken);
        await _packageProvider.DidNotReceiveWithAnyArgs().GetPackageAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    private static string[] Names(FunctionClosure closure) => [.. closure.Functions.Select(function => function.Function.Name)];

    /// <summary>
    /// Publishes a package in the fake gallery and returns its catalog.
    /// </summary>
    private FunctionCatalog AddPackage(
        string id,
        string version,
        (string Id, string Version)[] dependencies,
        params (string Name, string Expression)[] functions)
    {
        var tmdl = new StringBuilder();

        foreach (var (name, expression) in functions)
            tmdl.Append("function '").Append(name).Append("' = ").Append(expression).Append("\n\n");

        var package = new FakePackage(id, version, dependencies, tmdl.ToString());

        if (!_gallery.TryGetValue(id, out var versions))
            _gallery[id] = versions = [];

        versions[version] = package;

        using var opened = package.Open();
        return FunctionCatalog.Create(opened);
    }

    private sealed record FakePackage(string Id, string Version, (string Id, string Version)[] Dependencies, string Tmdl)
    {
        public Package Open()
        {
            var builder = PackageBuilder.Create(Id, Version)
                .WithDescription("test")
                .AddDocument(LibraryDocument.Create(PackagingConstants.TmdlFunctionsFilePath, Tmdl));

            foreach (var (dependencyId, dependencyVersion) in Dependencies)
                builder = builder.AddDependency(PackageDependency.Create(dependencyId, dependencyVersion));

            return Package.Open(builder.Build());
        }
    }
}
