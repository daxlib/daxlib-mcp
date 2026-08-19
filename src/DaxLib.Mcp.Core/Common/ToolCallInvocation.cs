using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// Wraps a tool call with uniform diagnostics and error handling. Every exception except
/// cancellation is converted into a failure result, so the SDK never replaces it with its own
/// generic error.
/// </summary>
internal sealed partial class ToolCallInvocation(ILogger<ToolCallInvocation> logger)
{
    private readonly ILogger<ToolCallInvocation> _logger = logger;

    public async Task<CallToolResult> RunAsync(string toolName, Func<Task<ToolOutcome>> operation)
    {
        var correlationId = Guid.NewGuid().ToString("N");

        try
        {
            var outcome = await operation();
            return Complete(
                toolName,
                correlationId,
                outcome,
                outcome.IsError ? "Failure" : "Success");
        }
        catch (OperationCanceledException)
        {
            LogToolCallCompleted(
                toolName,
                correlationId,
                "Cancelled",
                errorType: null);

            throw;
        }
        catch (Exception exception)
        {
            var errorType = exception.GetType().Name;
            LogUnexpectedFailure(toolName, correlationId, errorType, exception);

            return Complete(
                toolName,
                correlationId,
                Unexpected(exception, correlationId),
                "UnexpectedFailure",
                errorType);
        }
    }

    private static ToolOutcome Unexpected(Exception exception, string correlationId)
    {
        // TODO: Evaluate replacing exception.Message with a sanitized message if exception content
        // proves too sensitive for the client LLM. Keep it for now because it can contain important
        // recovery information.
        return ToolOutcome.Failed(
            $"The call failed with an unexpected error ({exception.GetType().Name}): {exception.Message} " +
            $"Correlation ID: {correlationId}.");
    }

    private CallToolResult Complete(
        string toolName,
        string correlationId,
        ToolOutcome outcome,
        string diagnosticOutcome,
        string? errorType = null)
    {
        LogToolCallCompleted(
            toolName,
            correlationId,
            diagnosticOutcome,
            errorType);

        return ToolResultFactory.Create(outcome);
    }
}
