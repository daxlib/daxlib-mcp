using System.Runtime.Versioning;
using Windows.Wdk.System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace DaxLib.Mcp.Core.Infrastructure.Interop;

/// <summary>
/// Provides interop with Windows processes.
/// </summary>
[SupportedOSPlatform("windows5.1.2600")]
internal static class ProcessInterop
{
    /// <summary>
    /// Returns the process ID of the process that created <paramref name="processId"/>,
    /// or <see langword="null"/> when the information cannot be obtained.
    /// </summary>
    public static unsafe int? GetParentProcessId(int processId)
    {
        using var process = Windows.Win32.PInvoke.OpenProcess_SafeHandle(
            PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION,
            bInheritHandle: false,
            (uint)processId);

        if (process.IsInvalid)
            return null;

        PROCESS_BASIC_INFORMATION information = default;
        uint returnLength = 0;

        var status = Windows.Wdk.PInvoke.NtQueryInformationProcess(
            (HANDLE)process.DangerousGetHandle(),
            PROCESSINFOCLASS.ProcessBasicInformation,
            &information,
            (uint)sizeof(PROCESS_BASIC_INFORMATION),
            ref returnLength);

        if (status.SeverityCode != NTSTATUS.Severity.Success)
            return null;

        // The parent may have exited and its PID may have been reused by another process.
        // The caller must therefore treat this PID as a hint and verify the process
        // identity before relying on it.
        return (int)information.InheritedFromUniqueProcessId;
    }
}
