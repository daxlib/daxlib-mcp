using System;
using System.Net;
using System.Net.Sockets;
using DaxLib.Mcp.Core.Infrastructure.Interop;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.Infrastructure.Interop;

/// <summary>
/// Covers the CsWin32 interop against a listener opened by the test, so that the generated
/// <c>GetExtendedTcpTable</c> signature and the row layout are verified without Power BI Desktop.
/// </summary>
/// <remarks>
/// The platform check is repeated inline in each test rather than extracted: it doubles as the
/// CA1416 guard, and the analyzer only reads it when it sits in the method making the call.
/// </remarks>
public sealed class TcpTableInteropTests
{
    private const string WindowsOnly = "GetExtendedTcpTable is Windows-only.";

    [Fact]
    public void GetIpv4Listeners_OpenListener_ReportsItsPortAndProcess()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
        {
            Assert.Skip(WindowsOnly);
            return;
        }

        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var listeners = TcpTableInterop.GetIpv4Listeners();

        Assert.NotNull(listeners);
        Assert.Contains((port, Environment.ProcessId), listeners);
    }

    [Fact]
    public void GetIpv4Listeners_EveryRow_CarriesAPortAndAProcessInRange()
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
        {
            Assert.Skip(WindowsOnly);
            return;
        }

        var listeners = TcpTableInterop.GetIpv4Listeners();

        Assert.NotNull(listeners);
        Assert.NotEmpty(listeners);

        // A byte-order or row-stride mistake shows up here as ports outside the 16-bit range or as
        // negative process ids.
        Assert.All(listeners, listener =>
        {
            Assert.InRange(listener.Port, 1, ushort.MaxValue);
            Assert.InRange(listener.ProcessId, 0, int.MaxValue);
        });
    }
}
