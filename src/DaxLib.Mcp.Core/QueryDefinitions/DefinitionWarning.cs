using System.Collections.Generic;

namespace DaxLib.Mcp.Core.QueryDefinitions;

/// <summary>
/// The warning codes of <c>get_function_definitions</c>. The strings are the wire contract.
/// </summary>
internal static class DefinitionWarningCodes
{
    public const string MissingDependency = "missingDependency";
    public const string DependencyNotFound = "dependencyNotFound";
    public const string DependencyVersionNotFound = "dependencyVersionNotFound";
    public const string VersionConflict = "versionConflict";
    public const string FunctionNotFound = "functionNotFound";
    public const string UndeclaredDependency = "undeclaredDependency";
    public const string UnresolvedCall = "unresolvedCall";
    public const string PackageCycle = "packageCycle";

    /// <summary>
    /// True for the codes that leave a called function out of the block, so that the block is not
    /// complete. The others are information only.
    /// </summary>
    public static bool MakesIncomplete(string code) => code
        is MissingDependency
        or DependencyNotFound
        or DependencyVersionNotFound
        or VersionConflict
        or FunctionNotFound;
}

/// <summary>
/// A problem found while the call closure was built. The block is still returned.
/// </summary>
/// <param name="Function">The called function the warning is about, when it is about one.</param>
/// <param name="Callers">The included functions that cannot run correctly because of the warning.</param>
internal sealed record DefinitionWarning(
    string Code,
    string Message,
    string? Package,
    string? Version,
    IReadOnlyList<string>? Versions,
    IReadOnlyList<string>? PublishedVersions,
    string? Function,
    IReadOnlyList<string> Callers);
