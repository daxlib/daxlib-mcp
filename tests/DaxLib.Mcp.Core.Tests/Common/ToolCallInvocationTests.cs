using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Tests.Logging;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using Xunit;

namespace DaxLib.Mcp.Core.Tests.Common;

public sealed class ToolCallInvocationTests
{
    [Fact]
    public async Task RunAsync_Success_LogsCompletion()
    {
        var logger = new RecordingLogger<ToolCallInvocation>();
        var invocation = new ToolCallInvocation(logger);

        var result = await invocation.RunAsync("test_tool", () => Task.FromResult(ToolOutcome.Ok("done")));

        Assert.NotEqual(true, result.IsError);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("ToolCallCompleted", entry.EventId.Name);
        Assert.Contains("test_tool", entry.Message, StringComparison.Ordinal);
        Assert.Contains("Outcome: Success", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_ExpectedFailure_LogsFailureCompletionWithoutErrorLog()
    {
        var logger = new RecordingLogger<ToolCallInvocation>();
        var invocation = new ToolCallInvocation(logger);

        var result = await invocation.RunAsync(
            "test_tool",
            () => Task.FromResult(ToolOutcome.Failed("expected failure")));

        Assert.True(result.IsError);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("Outcome: Failure", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_UnexpectedException_LogsErrorWithException()
    {
        var logger = new RecordingLogger<ToolCallInvocation>();
        var invocation = new ToolCallInvocation(logger);
        var exception = new InvalidOperationException("diagnostic detail");

        var result = await invocation.RunAsync(
            "test_tool",
            () => Task.FromException<ToolOutcome>(exception));

        Assert.True(result.IsError);
        Assert.Contains(
            "diagnostic detail",
            string.Join("\n", result.Content.OfType<TextContentBlock>().Select(content => content.Text)),
            StringComparison.Ordinal);

        var error = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Error);
        Assert.Contains("InvalidOperationException", error.Message, StringComparison.Ordinal);
        Assert.Same(exception, error.Exception);

        var completion = Assert.Single(logger.Entries, entry => entry.EventId.Name == "ToolCallCompleted");
        Assert.Contains("Outcome: UnexpectedFailure", completion.Message, StringComparison.Ordinal);

        var correlationId = CorrelationId(error.Message);
        Assert.Contains(correlationId, completion.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_Cancelled_LogsCancellationAndRethrows()
    {
        var logger = new RecordingLogger<ToolCallInvocation>();
        var invocation = new ToolCallInvocation(logger);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invocation.RunAsync(
            "test_tool",
            () => Task.FromCanceled<ToolOutcome>(cancellation.Token)));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("Outcome: Cancelled", entry.Message, StringComparison.Ordinal);
    }

    private static string CorrelationId(string message)
    {
        const string prefix = "Correlation ID: ";
        var start = message.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
        var end = message.IndexOf(';', start);

        return message[start..end];
    }

}
