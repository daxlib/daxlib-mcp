using System;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Infrastructure.AnalysisServices;

[SupportedOSPlatform("windows6.0.6000")]
internal sealed partial class AnalysisServicesInstanceLocator
{
    [LoggerMessage(
        EventId = 3000,
        EventName = "InstanceDiscoveryCompleted",
        Level = LogLevel.Information,
        Message = "Analysis Services instance discovery completed. Engine processes: {EngineProcessCount}; valid instances: {InstanceCount}; Power BI Desktop instances: {PowerBiDesktopCount}.")]
    private partial void LogInstanceDiscoveryCompleted(
        int engineProcessCount,
        int instanceCount,
        int powerBiDesktopCount);

    [LoggerMessage(
        EventId = 3001,
        EventName = "InstanceDiscoveryFailed",
        Level = LogLevel.Warning,
        Message = "Analysis Services instance discovery failed. Error type: {ErrorType}.")]
    private partial void LogInstanceDiscoveryFailed(string errorType, Exception exception);

    [LoggerMessage(
        EventId = 3010,
        EventName = "ParentProcessResolutionFailed",
        Level = LogLevel.Debug,
        Message = "Failed to resolve the parent of the Analysis Services instance {ProcessId}.")]
    private partial void LogParentProcessResolutionFailed(int processId, Exception exception);

    [LoggerMessage(
        EventId = 3011,
        EventName = "ParentProcessInfoReadFailed",
        Level = LogLevel.Debug,
        Message = "Failed to read the parent of the Analysis Services instance {ProcessId} via the Process API.")]
    private partial void LogParentProcessInfoReadFailed(int processId, Exception exception);
}
