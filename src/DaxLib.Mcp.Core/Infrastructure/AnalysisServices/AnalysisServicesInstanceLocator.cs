using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using DaxLib.Mcp.Core.Infrastructure.Interop;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Infrastructure.AnalysisServices;

/// <inheritdoc cref="IAnalysisServicesInstanceLocator"/>
[SupportedOSPlatform("windows6.0.6000")]
internal sealed partial class AnalysisServicesInstanceLocator(ILogger<AnalysisServicesInstanceLocator> logger)
    : IAnalysisServicesInstanceLocator
{
    private const string EngineProcessName = "msmdsrv";

    private readonly ILogger<AnalysisServicesInstanceLocator> _logger = logger;

    public IReadOnlyList<AnalysisServicesInstance> FindInstances(CancellationToken cancellationToken)
    {
        var engines = Process.GetProcessesByName(EngineProcessName);

        if (engines.Length == 0)
        {
            LogInstanceDiscoveryCompleted(
                engineProcessCount: 0,
                instanceCount: 0,
                powerBiDesktopCount: 0);
            return [];
        }

        try
        {
            var portsByProcess = TcpTableInterop.GetIpv4Listeners()
                .ToLookup(listener => listener.ProcessId, listener => listener.Port);

            List<AnalysisServicesInstance> instances = [];

            foreach (var engine in engines)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // An engine normally listens on a single IPv4 port. If multiple ports are reported,
                // use the lowest port as a deterministic tie-breaker.
                var port = portsByProcess[engine.Id].Order().FirstOrDefault();

                if (port == 0)
                {
                    // An engine with no listener is starting or already stopping.
                    continue;
                }

                if (!TryGetProcessStartTime(engine, out var startTime))
                    continue;

                var (parentProcessName, parentProcessWindowTitle) = GetParentProcessInfo(engine.Id, startTime);

                var instance = new AnalysisServicesInstance(
                    ProcessId: engine.Id,
                    Port: port,
                    StartTime: startTime,
                    ParentProcessName: parentProcessName,
                    ParentProcessWindowTitle: parentProcessWindowTitle);

                instances.Add(instance);
            }

            var powerBiDesktopCount = instances.Count(instance => instance.IsPowerBIDesktop);
            LogInstanceDiscoveryCompleted(engines.Length, instances.Count, powerBiDesktopCount);

            return instances;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var errorType = exception.GetType().Name;
            LogInstanceDiscoveryFailed(errorType, exception);
            throw;
        }
        finally
        {
            foreach (var engine in engines)
                engine.Dispose();
        }
    }

    public AnalysisServicesInstance? FindInstance(string dataSource, CancellationToken cancellationToken) =>
        FindInstances(cancellationToken).SingleOrDefault(
            instance => string.Equals(instance.DataSource, dataSource, StringComparison.OrdinalIgnoreCase));

    private (string? ProcessName, string? WindowTitle) GetParentProcessInfo(int engineProcessId, DateTime engineStartTime)
    {
        using var parent = GetParentProcess(engineProcessId);

        if (parent is null)
            return (null, null);

        try
        {
            // The parent PID is a hint that may have been recycled: a process started after
            // the engine cannot be its parent, so its identity is discarded.
            if (parent.StartTime > engineStartTime)
                return (null, null);

            var processName = parent.ProcessName;
            var windowTitle = parent.MainWindowTitle;

            if (string.IsNullOrWhiteSpace(windowTitle))
                windowTitle = null;

            return (processName, windowTitle);
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            LogParentProcessInfoReadFailed(engineProcessId, exception);

            // Failing to read the parent process only loses the label; the instance is still returned.
            return (null, null);
        }
    }

    private Process? GetParentProcess(int processId)
    {
        var parentProcessId = ProcessInterop.GetParentProcessId(processId);

        if (parentProcessId is null)
            return null;

        try
        {
            return Process.GetProcessById(parentProcessId.Value);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            LogParentProcessResolutionFailed(processId, exception);

            return null;
        }
    }

    private static bool TryGetProcessStartTime(Process engine, out DateTime startTime)
    {
        try
        {
            startTime = engine.StartTime;
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            // Reading the start time can fail if the process has exited or is inaccessible.
            startTime = default;
            return false;
        }
    }
}
