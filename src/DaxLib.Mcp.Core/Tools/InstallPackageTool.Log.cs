using DaxLib.Mcp.Core.SdkModeling;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Tools;

internal sealed partial class InstallPackageTool
{
    [LoggerMessage(
        EventId = 7002,
        EventName = "PackageInstallCompleted",
        Level = LogLevel.Information,
        Message = "Package installation completed. Functions written: {FunctionCount}.")]
    private partial void LogPackageInstallCompleted(int functionCount);

    [LoggerMessage(
        EventId = 7003,
        EventName = "PackageInstallCompatibilityTooLow",
        Level = LogLevel.Warning,
        Message = "Package installation stopped because model compatibility is too low. Actual: {ActualLevel}; required: {MinimumLevel}.")]
    private partial void LogCompatibilityTooLow(int actualLevel, int minimumLevel);

    [LoggerMessage(
        EventId = 7004,
        EventName = "PackageInstallUnexpectedDatabaseCount",
        Level = LogLevel.Warning,
        Message = "Package installation stopped because the target hosted {DatabaseCount} databases instead of one.")]
    private partial void LogUnexpectedDatabaseCount(int databaseCount);

    [LoggerMessage(
        EventId = 7005,
        EventName = "PackageInstallFailed",
        Level = LogLevel.Warning,
        Message = "Package installation failed. Outcome: {Outcome}; error type: {ErrorType}.")]
    private partial void LogPackageInstallFailed(
        string outcome,
        string errorType,
        System.Exception exception);

    private void LogPackageInstallOutcome(PackageInstallOutcome outcome)
    {
        switch (outcome)
        {
            case PackageInstallOutcome.Installed installed:
                LogPackageInstallCompleted(installed.FunctionCount);
                break;

            case PackageInstallOutcome.CompatibilityTooLow compatibility:
                LogCompatibilityTooLow(compatibility.ActualLevel, compatibility.MinimumLevel);
                break;

            case PackageInstallOutcome.UnexpectedDatabaseCount databaseCount:
                LogUnexpectedDatabaseCount(databaseCount.DatabaseCount);
                break;

            case PackageInstallOutcome.PackageDeserializationFailed failure:
                LogFailure(nameof(PackageInstallOutcome.PackageDeserializationFailed), failure.Exception);
                break;

            case PackageInstallOutcome.ConnectionFailed failure:
                LogFailure(nameof(PackageInstallOutcome.ConnectionFailed), failure.Exception);
                break;

            case PackageInstallOutcome.SaveChangesFailed failure:
                LogFailure(nameof(PackageInstallOutcome.SaveChangesFailed), failure.Exception);
                break;
        }
    }

    private void LogFailure(string outcome, System.Exception exception)
    {
        var errorType = exception.GetType().Name;
        LogPackageInstallFailed(outcome, errorType, exception);
    }
}
