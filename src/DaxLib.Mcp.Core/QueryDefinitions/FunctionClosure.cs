using System.Collections.Generic;
using System.Linq;

namespace DaxLib.Mcp.Core.QueryDefinitions;

/// <summary>
/// The functions a DEFINE block contains, the packages they come from, and the warnings found while
/// the block was built; or the error that prevents the block.
/// </summary>
internal sealed record FunctionClosure
{
    private FunctionClosure()
    {
    }

    /// <summary>
    /// The packages with at least one included function: the dependency packages first, each after
    /// its own dependencies, then the requested package.
    /// </summary>
    public IReadOnlyList<ClosurePackage> Packages { get; private init; } = [];

    public IReadOnlyList<DefinitionWarning> Warnings { get; private init; } = [];

    public ClosureError? Error { get; private init; }

    /// <summary>True when no warning leaves a called function out of the block.</summary>
    public bool IsComplete => Warnings.All(warning => !DefinitionWarningCodes.MakesIncomplete(warning.Code));

    /// <summary>Every included function, in block order.</summary>
    public IEnumerable<ClosureFunction> Functions => Packages.SelectMany(package => package.Functions);

    public static FunctionClosure Built(IReadOnlyList<ClosurePackage> packages, IReadOnlyList<DefinitionWarning> warnings) =>
        new() { Packages = packages, Warnings = warnings };

    public static FunctionClosure Failed(ClosureError error) =>
        new() { Error = error };
}

internal sealed record ClosurePackage(FunctionCatalog Catalog, bool IsRequested, IReadOnlyList<ClosureFunction> Functions);

/// <param name="IsRequested">True for a seed function, false for one added because it is called.</param>
/// <param name="Calls">
/// The names the function calls, built-in functions excluded: the exact package function name
/// when the call resolves to one, otherwise the name as written.
/// </param>
internal sealed record ClosureFunction(
    CatalogFunction Function,
    FunctionCatalog Catalog,
    bool IsRequested,
    IReadOnlyList<string> Calls);

/// <summary>Why no DEFINE block can be built.</summary>
internal abstract record ClosureError
{
    private ClosureError()
    {
    }

    /// <summary>The included functions call each other in a cycle, which the engine rejects.</summary>
    /// <param name="Cycle">The functions of the cycle; the first one is repeated at the end.</param>
    public sealed record RecursiveCall(IReadOnlyList<string> Cycle) : ClosureError;

    /// <summary>Two included functions of different packages have the same name.</summary>
    public sealed record NameCollision(string Function, IReadOnlyList<string> Packages) : ClosureError;

    /// <summary>The TMDL of a dependency package could not be parsed.</summary>
    public sealed record TmdlParseFailed(string PackageId, string Version, System.Exception Exception) : ClosureError;
}
