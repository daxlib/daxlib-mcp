using System.Text.Json;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// A rendered tool result: the serialized JSON envelope, whether the tool failed, and optionally
/// a document carried as its own content block.
/// </summary>
/// <remarks>
/// Tool failures travel in the result with <c>isError</c> true, never as JSON-RPC errors, so the
/// model receives the content and can correct itself.
/// </remarks>
internal sealed record ToolOutcome
{
    private ToolOutcome(string text, string message, bool isError, RenderedResource? resource)
    {
        Text = text;
        Message = message;
        IsError = isError;
        Resource = resource;
    }

    /// <summary>The serialized <see cref="ToolResponse"/> envelope.</summary>
    public string Text { get; }

    /// <summary>The message inside the envelope, kept separate for diagnostics.</summary>
    public string Message { get; }

    public bool IsError { get; }

    public RenderedResource? Resource { get; }

    public static ToolOutcome Ok(string message) =>
        new(Serialize(new ToolResponse(Success: true, message)), message, isError: false, resource: null);

    public static ToolOutcome Ok<TData>(string message, TData data) =>
        new(Serialize(new ToolResponse<TData>(Success: true, message, data)), message, isError: false, resource: null);

    public static ToolOutcome Ok(string message, RenderedResource resource) =>
        new(Serialize(new ToolResponse(Success: true, message)), message, isError: false, resource);

    public static ToolOutcome Ok<TData>(string message, TData data, RenderedResource resource) =>
        new(Serialize(new ToolResponse<TData>(Success: true, message, data)), message, isError: false, resource);

    public static ToolOutcome Failed(string message) =>
        new(Serialize(new ToolResponse(Success: false, message)), message, isError: true, resource: null);

    public static ToolOutcome Failed<TData>(string message, TData data) =>
        new(Serialize(new ToolResponse<TData>(Success: false, message, data)), message, isError: true, resource: null);

    private static string Serialize<TResponse>(TResponse response) =>
        JsonSerializer.Serialize(response, typeof(TResponse), ToolResponseSerializerContext.Default);
}
