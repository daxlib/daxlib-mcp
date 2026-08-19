using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Infrastructure.AnalysisServices;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DaxLib.Mcp.Core.Tools;

/// <summary>
/// Asks the user, through the client, to confirm an installation before the model is written.
/// </summary>
internal static class InstallConfirmation
{
    public enum Outcome
    {
        Confirmed,
        Declined,

        /// <summary>The client does not support elicitation; the caller proceeds without it.</summary>
        Unsupported,
    }

    public static async Task<Outcome> RequestAsync(
        McpServer? server,
        string packageId,
        string packageVersion,
        AnalysisServicesInstance instance,
        CancellationToken cancellationToken)
    {
        if (server?.ClientCapabilities?.Elicitation is null)
            return Outcome.Unsupported;

        var target = instance.ParentProcessWindowTitle is { Length: > 0 } title
            ? $"'{title}' ({instance.DataSource})"
            : instance.DataSource;

        var request = new ElicitRequestParams
        {
            Message = $"Install DAX Lib package {packageId} version {packageVersion} into the Power BI Desktop instance {target}?",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["confirm"] = new ElicitRequestParams.BooleanSchema(),
                },
                Required = ["confirm"],
            },
        };

        var result = await server.ElicitAsync(request, cancellationToken);

        return IsConfirmed(result) ? Outcome.Confirmed : Outcome.Declined;
    }

    /// <summary>
    /// A response confirms the installation only when it is accepted and carries <c>confirm: true</c>.
    /// </summary>
    private static bool IsConfirmed(ElicitResult result) =>
        string.Equals(result.Action, "accept", StringComparison.Ordinal)
        && result.Content is { } content
        && content.TryGetValue("confirm", out var confirm)
        && confirm.ValueKind is JsonValueKind.True;
}
