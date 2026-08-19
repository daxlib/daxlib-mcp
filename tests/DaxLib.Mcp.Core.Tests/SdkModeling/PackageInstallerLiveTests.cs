using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DaxLib.Mcp.Core.Infrastructure.AnalysisServices;
using DaxLib.Mcp.Core.SdkModeling;
using DaxLib.Mcp.Core.Tests.Fixtures;
using DaxLib.Packaging;
using Microsoft.AnalysisServices.Tabular;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.SdkModeling;

/// <summary>
/// Installs a real package into a running Power BI Desktop model. These tests write to the model
/// that is open, so they run only when explicitly requested: set <c>DAXLIB_MCP_LIVE_TESTS=1</c>
/// and open a scratch model in Power BI Desktop first. If either is missing, every test
/// self-skips. They are never enabled in CI.
/// </summary>
[Trait("Category", "LiveDesktop")]
public sealed class PackageInstallerLiveTests
{
    private const string OptInVariable = "DAXLIB_MCP_LIVE_TESTS";

    [Fact]
    public void Install_SmallPackage_WritesEveryFunctionAndRepeatsCleanly()
    {
        var target = RequireScratchDesktopConnection();

        using var package = BuildPackage("eg.base64.functions.tmdl", "EG.Base64");
        var expected = ReadFixture("eg.base64.functions.tmdl");

        var first = InstallOrSkip(package, target);
        var installed = Assert.IsType<PackageInstallOutcome.Installed>(first);

        Assert.Equal(expected.Functions.Count, installed.FunctionCount);

        using var again = BuildPackage("eg.base64.functions.tmdl", "EG.Base64");
        var repeated = Assert.IsType<PackageInstallOutcome.Installed>(InstallOrSkip(again, target));

        Assert.Equal(installed.FunctionCount, repeated.FunctionCount);

        WithConnectedModel(target, model =>
        {
            foreach (var function in expected.Functions)
            {
                var actual = model.Functions.Find(function.Name);

                Assert.NotNull(actual);
                Assert.Equal(function.Expression, actual.Expression);
                Assert.Equal("EG.Base64", actual.Annotations.Find(PackageId.AnnotationName)?.Value);
            }
        });
    }

    /// <summary>
    /// Acceptance case: installs the package that lost all 873 expression comments in the remote
    /// proof of concept, and verifies that every expression arrives intact.
    /// </summary>
    [Fact]
    public void Install_Ibcs_PreservesEveryExpressionComment()
    {
        var target = RequireScratchDesktopConnection();

        using var package = BuildPackage("powerofbi.ibcs.functions.tmdl", "PowerofBI.IBCS");
        var expected = ReadFixture("powerofbi.ibcs.functions.tmdl");

        _ = Assert.IsType<PackageInstallOutcome.Installed>(InstallOrSkip(package, target));

        WithConnectedModel(target, model =>
        {
            foreach (var function in expected.Functions)
                Assert.Equal(function.Expression, model.Functions.Find(function.Name)?.Expression);
        });
    }

    private static PackageInstallOutcome InstallOrSkip(Package package, ConnectionInfo connection)
    {
        var outcome = new PackageInstaller().Install(package, connection);

        if (outcome is PackageInstallOutcome.CompatibilityTooLow compatibility)
        {
            Assert.Skip(
                $"The open model's compatibility level is {compatibility.ActualLevel}; open a scratch model that supports user-defined functions (>= {compatibility.MinimumLevel}).");
        }

        return outcome;
    }

    private static Package BuildPackage(string fixture, string packageId) =>
        Package.Open(PackageBuilder.Create(packageId, "1.0.0")
            .WithDescription("daxlib-mcp live test")
            .AddDocument(LibraryDocument.Create(PackagingConstants.TmdlFunctionsFilePath, FixtureLoader.ReadText(fixture)))
            .Build());

    private static Model ReadFixture(string fixture)
    {
        using var content = FixtureLoader.Open(fixture);

        return PackageTmdlSerializer.DeserializeModel(
            [LibraryDocument.Create(PackagingConstants.TmdlFunctionsFilePath, content)]);
    }

    private static void WithConnectedModel(ConnectionInfo connection, Action<Model> assert)
    {
        using var server = new Server();
        server.Connect($"Data Source={connection.DataSource};Application Name={connection.ApplicationName}");

        try
        {
            assert(server.Databases[0].Model);
        }
        finally
        {
            server.Disconnect();
        }
    }

    private static ConnectionInfo RequireScratchDesktopConnection()
    {
        Assert.SkipWhen(
            Environment.GetEnvironmentVariable(OptInVariable) is not "1",
            $"Set {OptInVariable}=1 and open a scratch model in Power BI Desktop to run the live installer tests.");

        IReadOnlyList<AnalysisServicesInstance> instances = [];

        // The version check satisfies CA1416 for the Windows-only locator; 6.0.6000 is the
        // GetExtendedTcpTable minimum, and any machine that runs Power BI Desktop meets it.
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
            instances = new AnalysisServicesInstanceLocator(NullLogger<AnalysisServicesInstanceLocator>.Instance).FindInstances(CancellationToken.None);
        else
            Assert.Skip("Power BI Desktop only runs on Windows.");

        // The locator reports every local instance. These tests write, so they accept only a model
        // opened in Power BI Desktop by the developer.
        AnalysisServicesInstance[] desktopInstances = [.. instances.Where(
            instance => instance.IsPowerBIDesktop)];

        Assert.SkipWhen(desktopInstances.Length == 0, "No running Power BI Desktop instance was found.");
        Assert.SkipWhen(desktopInstances.Length > 1, "More than one Desktop instance is running; open only the scratch model.");

        return new ConnectionInfo(desktopInstances[0].DataSource, "daxlib-mcp-tests");
    }
}
