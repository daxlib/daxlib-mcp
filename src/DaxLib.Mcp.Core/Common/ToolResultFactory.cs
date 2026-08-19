using System.Collections.Generic;
using ModelContextProtocol.Protocol;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// Maps a rendered <see cref="ToolOutcome"/> onto the MCP <see cref="CallToolResult"/>. It never
/// renders text itself, so the renderers stay testable without the protocol types.
/// </summary>
internal static class ToolResultFactory
{
    public static CallToolResult Create(ToolOutcome outcome)
    {
        List<ContentBlock> content = [new TextContentBlock { Text = outcome.Text }];

        if (outcome.Resource is { } resource)
        {
            content.Add(new EmbeddedResourceBlock
            {
                Resource = new TextResourceContents
                {
                    Uri = resource.Uri,
                    MimeType = resource.MimeType,
                    Text = resource.Text,
                },
                Annotations = new Annotations
                {
                    // The document is addressed to both audiences: the user acts on it, the model relays it.
                    Audience = [Role.User, Role.Assistant],
                    LastModified = resource.LastModified,
                    // Priority stays unset: Codex rejects a fractional priority with "Unexpected response type".
                    // https://github.com/openai/codex/issues/38979
                },
            });
        }

        return new CallToolResult
        {
            Content = content,
            IsError = outcome.IsError,
        };
    }
}
