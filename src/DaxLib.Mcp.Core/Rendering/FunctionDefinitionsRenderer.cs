using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.QueryDefinitions;

namespace DaxLib.Mcp.Core.Rendering;

/// <summary>
/// Renders a <c>get_function_definitions</c> result: the JSON envelope with the closure as typed
/// data, and the DEFINE block as a text resource.
/// </summary>
internal static class FunctionDefinitionsRenderer
{
    private const string TextMediaType = "text/plain";

    private const string UriScheme = "daxlib-define";

    public static ToolOutcome Render(GalleryPackage package, IncludeDependencies mode, FunctionClosure closure, string block)
    {
        var packageId = package.Id.ToNormalizedString();
        var version = package.Version.ToNormalizedString();
        var functionCount = closure.Functions.Count();
        var warningCount = closure.Warnings.Count;

        var message =
            $"{packageId} {version}: {Count(functionCount, "function")} from {Count(closure.Packages.Count, "package")}. " +
            $"{Count(warningCount, "warning")}{(closure.IsComplete ? "" : ": the block is not complete")}. " +
            "The DEFINE block is attached.";

        var data = new FunctionDefinitionsData(
            Id: packageId,
            Version: version,
            IncludeDependencies: DependencyResolver.ToWireName(mode),
            Complete: closure.IsComplete,
            Packages: [.. closure.Packages.Select(ToPackageData)],
            Functions: [.. closure.Functions.Select(ToFunctionData)],
            Warnings: [.. closure.Warnings.Select(ToWarningData)],
            Characters: block.Length);

        var resource = new RenderedResource(
            $"{UriScheme}://{packageId}/{version}/define.dax",
            TextMediaType,
            block,
            package.Published);

        return ToolOutcome.Ok(message, data, resource);
    }

    public static ToolOutcome Render(ClosureError error) => error switch
    {
        ClosureError.RecursiveCall recursion => ToolOutcome.Failed(
            $"The functions call each other in a cycle: {string.Join(" → ", recursion.Cycle)}. " +
            "The DAX engine does not allow recursion in user-defined functions, so no DEFINE block was built.",
            new FunctionDefinitionsErrorData("recursiveCall", Functions: recursion.Cycle)),

        ClosureError.NameCollision collision => ToolOutcome.Failed(
            $"Function {collision.Function} is in more than one package ({string.Join(", ", collision.Packages)}). " +
            "A DEFINE block cannot contain the same name twice, so no block was built.",
            new FunctionDefinitionsErrorData("nameCollision", Functions: [collision.Function])),

        ClosureError.TmdlParseFailed failure => TmdlParseFailed(failure.PackageId, failure.Version, failure.Exception),

        _ => throw new UnreachableException($"Unhandled {nameof(ClosureError)}: {error.GetType().Name}."),
    };

    public static ToolOutcome TmdlParseFailed(string packageId, string version, Exception exception) =>
        ToolOutcome.Failed(
            $"Package {packageId} version {version} could not be read. " +
            $"The parser reported {exception.GetType().Name}: {exception.Message}. " +
            "The package content itself is invalid, so retrying will not help.",
            new FunctionDefinitionsErrorData("tmdlParseFailed"));

    public static ToolOutcome UnknownFunctions(
        GalleryPackage package,
        IReadOnlyList<string> unknown,
        IReadOnlyList<string> suggestions)
    {
        var packageId = package.Id.ToNormalizedString();
        var version = package.Version.ToNormalizedString();
        var names = string.Join(", ", unknown.Select(name => $"'{name}'"));
        var noun = unknown.Count == 1 ? "Function" : "Functions";
        var verb = unknown.Count == 1 ? "is" : "are";

        var message = suggestions.Count > 0
            ? $"{noun} {names} {verb} not in package {packageId} version {version}. " +
              $"Close names: {string.Join(", ", suggestions)}. Call this tool again with the exact names, or omit `functions` to get every function."
            : $"{noun} {names} {verb} not in package {packageId} version {version}. Omit `functions` to get every function of the package.";

        return ToolOutcome.Failed(message, new FunctionDefinitionsErrorData("unknownFunction", unknown, suggestions));
    }

    public static ToolOutcome EmptyFunctionList() =>
        ToolOutcome.Failed("`functions` must contain at least one function name. Omit it to get every function of the package.");

    public static ToolOutcome NoFunctions(GalleryPackage package) =>
        ToolOutcome.Failed(
            $"Package {package.Id.ToNormalizedString()} version {package.Version.ToNormalizedString()} contains no functions, so there is no DEFINE block.");

    private static FunctionDefinitionsPackageData ToPackageData(ClosurePackage package) => new(
        package.Catalog.PackageId,
        package.Catalog.Version.ToNormalizedString(),
        package.IsRequested ? "requested" : "dependency",
        [.. package.Functions.Select(function => function.Function.Name)]);

    private static FunctionDefinitionData ToFunctionData(ClosureFunction function) => new(
        function.Function.Name,
        function.Catalog.PackageId,
        function.Catalog.Version.ToNormalizedString(),
        function.Function.Description,
        [.. function.Function.Parameters.Select(parameter => new FunctionParameterData(
            parameter.Name, parameter.Type, parameter.Optional, parameter.Description))],
        function.Calls,
        function.IsRequested ? "requested" : "dependency");

    private static FunctionDefinitionWarningData ToWarningData(DefinitionWarning warning) => new(
        warning.Code,
        warning.Message,
        warning.Package,
        warning.Version,
        warning.Versions,
        warning.PublishedVersions,
        warning.Function,
        warning.Callers);

    private static string Count(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
