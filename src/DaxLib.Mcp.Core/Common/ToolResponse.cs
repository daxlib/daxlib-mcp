namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// The JSON envelope every tool result serializes into its text block: <c>success</c>, a
/// human-readable <c>message</c>, and on the generic form a typed <c>data</c> payload.
/// <c>success</c> repeats the protocol's <c>isError</c> inside the text, because the text is what
/// the model reads.
/// </summary>
/// <remarks>
/// The generic and non-generic forms share this file because they are one wire contract.
/// </remarks>
internal sealed record ToolResponse(bool Success, string Message);

/// <inheritdoc cref="ToolResponse"/>
internal sealed record ToolResponse<TData>(bool Success, string Message, TData Data);
