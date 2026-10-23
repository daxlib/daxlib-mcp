using System.Collections.Generic;

namespace DaxLib.Mcp.Core.Common;

/// <summary>
/// Data returned by <c>get_function_definitions</c>. The property names are the wire contract.
/// The DEFINE block itself is the embedded resource, not part of the data.
/// </summary>
/// <param name="IncludeDependencies">The mode used: <c>none</c>, <c>package</c> or <c>all</c>.</param>
/// <param name="Complete">False when a called function is not in the block.</param>
/// <param name="Characters">The length of the DEFINE block.</param>
internal sealed record FunctionDefinitionsData(
    string Id,
    string Version,
    string IncludeDependencies,
    bool Complete,
    IReadOnlyList<FunctionDefinitionsPackageData> Packages,
    IReadOnlyList<FunctionDefinitionData> Functions,
    IReadOnlyList<FunctionDefinitionWarningData> Warnings,
    int Characters);

/// <param name="Version">Always an exact version: the requested one, or the one a manifest declares.</param>
/// <param name="Role"><c>requested</c> or <c>dependency</c>.</param>
internal sealed record FunctionDefinitionsPackageData(
    string Id,
    string Version,
    string Role,
    IReadOnlyList<string> Functions);

/// <param name="Reason"><c>requested</c>, or <c>dependency</c> when the function is in the block because it is called.</param>
internal sealed record FunctionDefinitionData(
    string Name,
    string Package,
    string Version,
    string? Description,
    IReadOnlyList<FunctionParameterData> Parameters,
    IReadOnlyList<string> Calls,
    string Reason);

internal sealed record FunctionParameterData(
    string Name,
    string Type,
    bool Optional,
    string? Description);

internal sealed record FunctionDefinitionWarningData(
    string Code,
    string Message,
    string? Package,
    string? Version,
    IReadOnlyList<string>? Versions,
    IReadOnlyList<string>? PublishedVersions,
    string? Function,
    IReadOnlyList<string> Callers);

/// <summary>
/// Data returned with a <c>get_function_definitions</c> failure that the tool itself detects, so
/// that the client can act on the code.
/// </summary>
/// <param name="Code"><c>unknownFunction</c>, <c>tmdlParseFailed</c>, <c>recursiveCall</c> or <c>nameCollision</c>.</param>
/// <param name="Functions">The functions the error is about: the unknown names, the cycle, or the colliding name.</param>
/// <param name="Suggestions">For <c>unknownFunction</c>: up to 10 close names of the package.</param>
internal sealed record FunctionDefinitionsErrorData(
    string Code,
    IReadOnlyList<string>? Functions = null,
    IReadOnlyList<string>? Suggestions = null);
