using System.Text.Json.Serialization;

namespace DaxLib.Mcp.Core.QueryDefinitions;

/// <summary>
/// Which called functions <c>get_function_definitions</c> adds to the requested ones. The JSON
/// names are the wire contract.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<IncludeDependencies>))]
internal enum IncludeDependencies
{
    /// <summary>Only the requested functions.</summary>
    [JsonStringEnumMemberName("none")]
    None,

    /// <summary>Also the functions they call in the same package.</summary>
    [JsonStringEnumMemberName("package")]
    Package,

    /// <summary>Also the functions they call in the declared dependency packages, at the declared versions.</summary>
    [JsonStringEnumMemberName("all")]
    All,
}
