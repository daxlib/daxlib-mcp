using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.IpHelper;
using Windows.Win32.Networking.WinSock;

namespace DaxLib.Mcp.Core.Infrastructure.Interop;

/// <summary>
/// Interop with the Windows TCP table API.
/// </summary>
[SupportedOSPlatform("windows6.0.6000")]
internal static class TcpTableInterop
{
    /// <summary>
    /// Returns every (port, process id) pair for IPv4 listeners on this machine.
    /// One entry per socket: the same process can appear more than once.
    /// </summary>
    public static List<(int Port, int ProcessId)> GetIpv4Listeners()
        => GetListeners((uint)ADDRESS_FAMILY.AF_INET);

    private static List<(int Port, int ProcessId)> GetListeners(uint addressFamily)
    {
        uint size = 0;
        var error = QueryTable([], ref size, addressFamily);

        // Match IPGlobalProperties: keep resizing until Windows returns a complete snapshot.
        while (error == (uint)WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
        {
            var buffer = new byte[size];
            error = QueryTable(buffer, ref size, addressFamily);

            if (error == (uint)WIN32_ERROR.NO_ERROR)
                return ParseTcpTableBuffer(buffer);
        }

        return [];
    }

    private static List<(int Port, int ProcessId)> ParseTcpTableBuffer(byte[] buffer)
    {
        ref var table = ref MemoryMarshal.AsRef<MIB_TCPTABLE_OWNER_PID>(buffer.AsSpan());
        var rows = table.table.AsSpan((int)table.dwNumEntries);

        List<(int Port, int ProcessId)> listeners = new(rows.Length);

        foreach (ref readonly var row in rows)
        {
            var port = BinaryPrimitives.ReverseEndianness((ushort)row.dwLocalPort);
            var pid = (int)row.dwOwningPid;

            listeners.Add((port, pid));
        }

        return listeners;
    }

    private static uint QueryTable(Span<byte> buffer, ref uint size, uint addressFamily) =>
        PInvoke.GetExtendedTcpTable(
            buffer,
            ref size,
            bOrder: false,
            addressFamily,
            TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER,
            Reserved: 0);
}
