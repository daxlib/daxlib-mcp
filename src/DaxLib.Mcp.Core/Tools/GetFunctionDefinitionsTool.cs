using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Common;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Mcp.Core.PackageLookup;
using DaxLib.Mcp.Core.QueryDefinitions;
using DaxLib.Mcp.Core.Rendering;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DaxLib.Mcp.Core.Tools;

[McpServerToolType]
internal sealed partial class GetFunctionDefinitionsTool(
    PackageResolver packageResolver,
    IPackageProvider packageProvider,
    DependencyResolver dependencyResolver,
    ToolCallInvocation toolCallInvocation,
    ILogger<GetFunctionDefinitionsTool> logger)
{
    private const int MaximumSuggestions = 10;

    private readonly PackageResolver _packageResolver = packageResolver;
    private readonly IPackageProvider _packageProvider = packageProvider;
    private readonly DependencyResolver _dependencyResolver = dependencyResolver;
    private readonly ToolCallInvocation _toolCallInvocation = toolCallInvocation;
    private readonly ILogger<GetFunctionDefinitionsTool> _logger = logger;

    [McpServerTool(
        Name = ToolNames.GetFunctionDefinitions,
        Title = "Get DAX query definitions for package functions",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = true)]
    [Description(
        $"""
        Returns a DAX query DEFINE block with the functions of a DAX Lib package and the functions they call, so you can use them inside a DAX query without changing the model.
        Paste the block before EVALUATE. Does not connect to Power BI.
        Functions in the DEFINE block replace model functions with the same name for this query only.
        A few functions published before a DAX parser change cannot run in a query; the query error points to the name part.
        """)]
    public Task<CallToolResult> GetFunctionDefinitionsAsync(
        [Description("Exact package id, e.g. 'DaxLib.SVG'. Case-insensitive.")]
        string id,
        [Description("Exact package version. If omitted, the latest published version is used, prerelease included.")]
        string? version = null,
        [Description("Functions to return, e.g. ['DaxLib.SVG.Viz.Pill']. If omitted, all functions of the package.")]
        string[]? functions = null,
        [Description(
            "none: only the listed functions. package: add the functions they call in the same package. " +
            "all: also add the functions they call in the packages declared in the manifest, at the declared versions.")]
        IncludeDependencies includeDependencies = IncludeDependencies.All,
        [Description("Write each function description as // comments above the function.")]
        bool includeDescriptions = true,
        CancellationToken cancellationToken = default)
    {
        return _toolCallInvocation.RunAsync(ToolNames.GetFunctionDefinitions, async () =>
        {
            if (functions is { Length: 0 })
                return FunctionDefinitionsRenderer.EmptyFunctionList();

            var resolution = await _packageResolver.ResolveAsync(id, version, cancellationToken);

            if (!resolution.IsResolved)
                return PackageLookupRenderer.Render(resolution.Failure);

            var resolvedPackage = resolution.Package;
            var packageId = resolvedPackage.Id.ToNormalizedString();
            var packageVersion = resolvedPackage.Version.ToNormalizedString();

            FunctionCatalog catalog;

            using (var package = await _packageProvider.GetPackageAsync(resolvedPackage.Id, resolvedPackage.Version, cancellationToken))
            {
                if (package is null)
                    return PackageLookupRenderer.Render(
                        new PackageLookupFailure.UnknownVersion(resolvedPackage.Id, packageVersion, resolution.Versions));

                try
                {
                    catalog = FunctionCatalog.Create(package);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    return FunctionDefinitionsRenderer.TmdlParseFailed(packageId, packageVersion, exception);
                }
            }

            if (catalog.Functions.Count == 0)
                return FunctionDefinitionsRenderer.NoFunctions(resolvedPackage);

            var seeds = SelectSeeds(catalog, functions, out var unknown);

            if (unknown.Count > 0)
            {
                IReadOnlyList<string> suggestions =
                [
                    .. unknown
                        .SelectMany(name => catalog.SuggestNames(name, MaximumSuggestions))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(MaximumSuggestions),
                ];

                return FunctionDefinitionsRenderer.UnknownFunctions(resolvedPackage, unknown, suggestions);
            }

            var closure = await _dependencyResolver.ResolveAsync(catalog, seeds, includeDependencies, cancellationToken);

            if (closure.Error is { } error)
                return FunctionDefinitionsRenderer.Render(error);

            CatalogFunction[] blockFunctions = [.. closure.Functions.Select(function => function.Function)];
            var block = DaxQueryWriter.Write(blockFunctions, includeDescriptions);
            var isComplete = closure.IsComplete;

            LogDefinitionsCompleted(
                blockFunctions.Length,
                closure.Packages.Count,
                closure.Warnings.Count,
                isComplete,
                block.Length);

            return FunctionDefinitionsRenderer.Render(resolvedPackage, includeDependencies, closure, block);
        });
    }

    private static List<CatalogFunction> SelectSeeds(FunctionCatalog catalog, string[]? names, out List<string> unknown)
    {
        unknown = [];

        if (names is null)
            return [.. catalog.Functions];

        List<CatalogFunction> seeds = [];

        // Tool arguments are deserialized from client JSON, so an array element can still arrive null.
        foreach (var name in names.Select(name => name?.Trim() ?? string.Empty))
        {
            if (catalog.TryGetFunction(name, out var function))
            {
                if (!seeds.Contains(function))
                    seeds.Add(function);
            }
            else if (!unknown.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                unknown.Add(name);
            }
        }

        return seeds;
    }
}
