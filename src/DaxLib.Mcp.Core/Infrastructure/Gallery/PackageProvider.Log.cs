using System;
using DaxLib.Packaging;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Infrastructure.Gallery;

internal sealed partial class PackageProvider
{
    [LoggerMessage(
        EventId = 5000,
        EventName = "PackageAcquisitionCompleted",
        Level = LogLevel.Information,
        Message = "Package acquisition completed for {PackageId} {PackageVersion}. Outcome: {Outcome}; downloaded bytes: {DownloadedBytes}.")]
    private partial void LogPackageAcquisitionCompleted(
        PackageId packageId,
        PackageVersion packageVersion,
        string outcome,
        long downloadedBytes);

    [LoggerMessage(
        EventId = 5001,
        EventName = "PackageAcquisitionFailed",
        Level = LogLevel.Warning,
        Message = "Package acquisition failed for {PackageId} {PackageVersion}. Error type: {ErrorType}.")]
    private partial void LogPackageAcquisitionFailed(
        PackageId packageId,
        PackageVersion packageVersion,
        string errorType,
        Exception exception);
}
