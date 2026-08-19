namespace DaxLib.Mcp.Core.SdkModeling;

/// <summary>
/// Information about a connection to a model.
/// </summary>
internal sealed record ConnectionInfo(
    string DataSource,
    string ApplicationName);
