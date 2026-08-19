using System;
using DaxLib.Packaging;
using Microsoft.AnalysisServices.Tabular;
using AmoException = Microsoft.AnalysisServices.AmoException;
using ConnectionException = Microsoft.AnalysisServices.ConnectionException;

namespace DaxLib.Mcp.Core.SdkModeling;

/// <inheritdoc cref="IPackageInstaller"/>
internal sealed class PackageInstaller : IPackageInstaller
{
    /// <summary>The lowest compatibility level that supports user-defined functions.</summary>
    public const int MinimumCompatibilityLevel = 1702;

    public PackageInstallOutcome Install(Package package, ConnectionInfo connectionInfo)
    {
        Model sourceModel;

        try
        {
            sourceModel = PackageTmdlSerializer.DeserializeModel(package);
        }
        catch (Exception ex)
        {
            return new PackageInstallOutcome.PackageDeserializationFailed(ex);
        }

        var connectionString = $"Data Source={connectionInfo.DataSource};Application Name={connectionInfo.ApplicationName}";
        var server = new Server();

        try
        {
            server.Connect(connectionString);

            // A Power BI Desktop instance hosts exactly one database; any other count means
            // the model is not loaded or the target is not a Desktop instance.
            if (server.Databases.Count != 1)
                return new PackageInstallOutcome.UnexpectedDatabaseCount(server.Databases.Count);

            var database = server.Databases[0];

            if (database.CompatibilityLevel < MinimumCompatibilityLevel)
                return new PackageInstallOutcome.CompatibilityTooLow(
                    database.CompatibilityLevel, MinimumCompatibilityLevel);

            var model = database.Model;

            foreach (var function in sourceModel.Functions)
                Write(model, function);

            _ = model.SaveChanges();

            return new PackageInstallOutcome.Installed(database.Name, sourceModel.Functions.Count);
        }
        catch (ConnectionException exception)
        {
            return new PackageInstallOutcome.ConnectionFailed(exception);
        }
        catch (AmoException exception)
        {
            return new PackageInstallOutcome.SaveChangesFailed(exception);
        }
        finally
        {
            try
            {
                server.Dispose();
            }
            catch { /* Ignore exceptions during disposal. */ }
        }
    }

    /// <summary>
    /// Removes the existing function, if any, and adds the one from the package. The effect is that
    /// of a CREATE OR REPLACE.
    /// </summary>
    private static void Write(Model model, Function source)
    {
        var target = model.Functions.Find(source.Name);

        if (target is not null)
            model.Functions.Remove(target);

        model.Functions.Add(source.Clone());
    }
}
