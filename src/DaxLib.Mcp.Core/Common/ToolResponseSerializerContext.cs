using System.Text.Json.Serialization;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// Source-generated serialization for every <see cref="ToolResponse"/> that goes on the wire:
/// camelCase, nulls omitted, unindented. Every closed <see cref="ToolResponse{TData}"/> a tool serializes must be
/// registered here. An unregistered type fails the call at runtime rather than the build, so the
/// registration belongs in the change that adds the type.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ToolResponse))]
[JsonSerializable(typeof(ToolResponse<LocalInstanceData[]>))]
[JsonSerializable(typeof(ToolResponse<PackageSearchData[]>))]
[JsonSerializable(typeof(ToolResponse<PackageDetailsData>))]
[JsonSerializable(typeof(ToolResponse<PackageInstallData>))]
[JsonSerializable(typeof(ToolResponse<FunctionDefinitionsData>))]
[JsonSerializable(typeof(ToolResponse<FunctionDefinitionsErrorData>))]
internal sealed partial class ToolResponseSerializerContext : JsonSerializerContext;
