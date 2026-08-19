using System;

namespace DaxLib.Mcp.Core.SdkModeling;

/// <summary>
/// The outcome of an install attempt: success, or the reason the installation did not complete.
/// </summary>
internal abstract record PackageInstallOutcome
{
    // Private, so only the nested cases can derive and the set stays closed.
    private PackageInstallOutcome()
    {
    }

    public sealed record Installed(string DatabaseName, int FunctionCount) : PackageInstallOutcome;
    public sealed record CompatibilityTooLow(int ActualLevel, int MinimumLevel) : PackageInstallOutcome;
    public sealed record UnexpectedDatabaseCount(int DatabaseCount) : PackageInstallOutcome;
    public sealed record PackageDeserializationFailed(Exception Exception) : PackageInstallOutcome;
    public sealed record ConnectionFailed(Exception Exception) : PackageInstallOutcome;
    public sealed record SaveChangesFailed(Exception Exception) : PackageInstallOutcome;
}
