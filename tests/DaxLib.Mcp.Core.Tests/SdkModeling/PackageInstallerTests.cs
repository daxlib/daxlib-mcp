using System.Linq;
using DaxLib.Mcp.Core.SdkModeling;
using DaxLib.Mcp.Core.Tests.Fixtures;
using DaxLib.Packaging;
using Microsoft.AnalysisServices.Tabular;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.SdkModeling;

/// <summary>
/// The parts of the installer that work on an in-memory model, without a connection.
/// </summary>
public sealed class PackageInstallerTests
{
    [Fact]
    public void FindDependenciesNotInstalled_DependencyWithoutFunctions_IsReported()
    {
        using var sample = FixtureLoader.OpenPackage("DaxLib.Sample", "0.1.8");
        var model = new Model();
        AddFunction(model, "DaxLib.Sample.MyFunc1", "DaxLib.Sample");
        AddFunction(model, "My.UserFunction", packageId: null);

        var missing = PackageInstaller.FindDependenciesNotInstalled(model, sample.Manifest);

        var dependency = Assert.Single(missing);
        Assert.Equal("DaxLib.Records", dependency.PackageId.ToNormalizedString());
        Assert.Equal("0.9.0-beta", dependency.Version.ToNormalizedString());
    }

    [Fact]
    public void FindDependenciesNotInstalled_DependencyWithAFunctionInTheModel_IsNotReported()
    {
        using var sample = FixtureLoader.OpenPackage("DaxLib.Sample", "0.1.8");
        var model = new Model();
        AddFunction(model, "DaxLib.Records.Any", "daxlib.records");

        Assert.Empty(PackageInstaller.FindDependenciesNotInstalled(model, sample.Manifest));
    }

    [Fact]
    public void FindDependenciesNotInstalled_NoDependencies_ReportsNothing()
    {
        using var svg = FixtureLoader.OpenPackage("DaxLib.SVG", "2.0.0");

        Assert.Empty(PackageInstaller.FindDependenciesNotInstalled(new Model(), svg.Manifest).ToArray());
    }

    private static void AddFunction(Model model, string name, string? packageId)
    {
        var function = new Function { Name = name, Expression = "() => 1" };

        if (packageId is not null)
            function.Annotations.Add(new Annotation { Name = PackageId.AnnotationName, Value = packageId });

        model.Functions.Add(function);
    }
}
