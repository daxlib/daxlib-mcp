using System;
using System.Diagnostics;
using DaxLib.Mcp.Core.Infrastructure.Interop;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.Infrastructure.Interop;

/// <summary>
/// Covers the kernel parent-process lookup, which is the locator's only source of parent PIDs.
/// It runs against a child process started by the test.
/// </summary>
/// <remarks>
/// The platform check is repeated inline in each test rather than extracted: it doubles as the
/// CA1416 guard, and the analyzer only reads it when it sits in the method making the call.
/// </remarks>
public sealed class ProcessInteropTests
{
    private const string WindowsOnly = "NtQueryInformationProcess is Windows-only.";

    [Fact]
    public void GetParentProcessId_ChildProcess_ReportsThisProcess()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
        {
            Assert.Skip(WindowsOnly);
            return;
        }

        using var child = Process.Start(new ProcessStartInfo("cmd.exe", "/c ping 127.0.0.1 -n 20")
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        });

        Assert.NotNull(child);

        try
        {
            Assert.Equal(Environment.ProcessId, ProcessInterop.GetParentProcessId(child.Id));
        }
        finally
        {
            child.Kill(entireProcessTree: true);
            child.WaitForExit();
        }
    }

    [Fact]
    public void GetParentProcessId_UnknownProcess_ReturnsNull()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
        {
            Assert.Skip(WindowsOnly);
            return;
        }

        // Odd ids are never process ids on Windows: they are multiples of four.
        Assert.Null(ProcessInterop.GetParentProcessId(int.MaxValue));
    }
}
