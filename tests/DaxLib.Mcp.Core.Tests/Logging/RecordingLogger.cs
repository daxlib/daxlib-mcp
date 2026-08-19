using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DaxLib.Mcp.Core.Tests.Logging;

internal sealed record RecordedLog(LogLevel Level, EventId EventId, string Message, Exception? Exception);

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<RecordedLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add(new RecordedLog(logLevel, eventId, formatter(state, exception), exception));
}
