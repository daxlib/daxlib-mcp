namespace DaxLib.Mcp.Core.Infrastructure.Gallery;

internal sealed record SearchRequest(string Query, int Skip, int Limit, bool IncludePrerelease);
