using System;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// A document a tool returns as its own content block instead of including it in the text.
/// <see cref="ToolResultFactory"/> maps it onto an <c>EmbeddedResourceBlock</c>.
/// </summary>
/// <param name="Uri">
/// Identifies the document, e.g. <c>daxlib-readme://DaxLib.SVG/2.0.0/README.md</c>. It is only an
/// identifier: this server exposes no MCP resources.
/// </param>
internal sealed record RenderedResource(
    string Uri,
    string MimeType,
    string Text,
    DateTimeOffset LastModified);
