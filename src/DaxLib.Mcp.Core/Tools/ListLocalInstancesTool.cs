using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.AnalysisServices;
using DaxLib.Mcp.Core.Rendering;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DaxLib.Mcp.Core.Tools;

[McpServerToolType]
internal sealed class ListLocalInstancesTool(
    IAnalysisServicesInstanceLocator instanceLocator,
    ToolCallInvocation toolCallInvocation)
{
    private readonly IAnalysisServicesInstanceLocator _instanceLocator = instanceLocator;
    private readonly ToolCallInvocation _toolCallInvocation = toolCallInvocation;

    [McpServerTool(
        Name = ToolNames.ListLocalInstances,
        Title = "List Power BI Desktop instances",
        ReadOnly = true,
        Destructive = false,
        OpenWorld = false)]
    [Description(
        $"""
        List all Power BI Desktop instances currently running on the local machine, including their connection details.
        Name an instance by its `parentProcessWindowTitle` when present; the title is not unique — `dataSource` is the identifier.
        The user must choose the target instance; never select one automatically, even when only one is running.
        """)]
    public Task<CallToolResult> ListLocalInstancesAsync(CancellationToken cancellationToken)
    {
        return _toolCallInvocation.RunAsync(ToolNames.ListLocalInstances, () =>
        {
            // The locator reports every local instance; only Power BI Desktop is a supported target.
            var instances = _instanceLocator.FindInstances(cancellationToken)
                .Where(instance => instance.IsPowerBIDesktop)
                .ToArray();

            var result = LocalInstancesRenderer.Render(instances);

            return Task.FromResult(result);
        });
    }
}
