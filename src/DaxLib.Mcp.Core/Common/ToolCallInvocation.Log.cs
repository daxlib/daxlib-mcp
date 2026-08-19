using System;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Common;

internal sealed partial class ToolCallInvocation
{
    [LoggerMessage(
        EventId = 1000,
        EventName = "ToolCallCompleted",
        Level = LogLevel.Information,
        Message = "MCP tool {ToolName} completed. Correlation ID: {CorrelationId}; Outcome: {Outcome}; error type: {ErrorType}.")]
    private partial void LogToolCallCompleted(
        string toolName,
        string correlationId,
        string outcome,
        string? errorType);

    [LoggerMessage(
        EventId = 1001,
        EventName = "ToolCallUnexpectedFailure",
        Level = LogLevel.Error,
        Message = "MCP tool {ToolName} failed unexpectedly. Correlation ID: {CorrelationId}; error type: {ErrorType}.")]
    private partial void LogUnexpectedFailure(
        string toolName,
        string correlationId,
        string errorType,
        Exception exception);
}
