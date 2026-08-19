using System.Linq;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.AnalysisServices;

namespace DaxLib.Mcp.Core.Rendering;

internal static class LocalInstancesRenderer
{
    public static ToolOutcome Render(AnalysisServicesInstance[] instances)
    {
        var noun = instances.Length == 1 ? "instance" : "instances";
        var message = $"Found {instances.Length} local Power BI Desktop {noun}.";

        return ToolOutcome.Ok<LocalInstanceData[]>(
            message: message,
            data: [.. instances.Select(instance => new LocalInstanceData(
                ProcessId: instance.ProcessId,
                Port: instance.Port,
                StartTime: instance.StartTime,
                ParentProcessWindowTitle: instance.ParentProcessWindowTitle,
                DataSource: instance.DataSource))]);
    }
}
