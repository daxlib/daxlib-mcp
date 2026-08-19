using DaxLib.Packaging;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.PackageLookup;

internal sealed partial class PackageResolver
{
    [LoggerMessage(
        EventId = 4000,
        EventName = "PackageResolutionCompleted",
        Level = LogLevel.Information,
        Message = "Package resolution completed for {PackageId} {ResolvedVersion}; available versions: {VersionCount}.")]
    private partial void LogPackageResolutionCompleted(
        PackageId packageId,
        PackageVersion resolvedVersion,
        int versionCount);

    [LoggerMessage(
        EventId = 4001,
        EventName = "PackageResolutionUnknownPackage",
        Level = LogLevel.Information,
        Message = "Package resolution did not find '{RequestedId}'. Suggestions: {SuggestionCount}.")]
    private partial void LogUnknownPackage(
        string requestedId,
        int suggestionCount);

    [LoggerMessage(
        EventId = 4002,
        EventName = "PackageResolutionUnknownVersion",
        Level = LogLevel.Information,
        Message = "Package resolution did not find version '{RequestedVersion}' for {PackageId}. Available versions: {VersionCount}.")]
    private partial void LogUnknownVersion(
        PackageId packageId,
        string requestedVersion,
        int versionCount);
}
