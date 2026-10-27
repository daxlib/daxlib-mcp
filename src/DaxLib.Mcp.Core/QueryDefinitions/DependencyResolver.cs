using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DaxLib.Mcp.Core.Infrastructure.Gallery;
using DaxLib.Packaging;

namespace DaxLib.Mcp.Core.QueryDefinitions;

/// <summary>
/// Builds the call closure of a set of functions: the functions they call, recursively, in the
/// same package and in the packages that the manifests declare.
/// </summary>
/// <remarks>
/// <para>
/// Only exact versions are used: a dependency is loaded at the version its caller's manifest
/// declares, or not at all. When that version is not published, or two callers need two versions
/// of one package, the resolver picks no other version: it reports a warning and leaves the calls
/// unsatisfied. See <c>docs/decisions/dependency-resolution.md</c>.
/// </para>
/// <para>
/// A dependency package is downloaded only when a call goes into it, and once per resolution.
/// Every published function name starts with its package id, so the target package is known from
/// the name before any download.
/// </para>
/// </remarks>
internal sealed class DependencyResolver(IGalleryClient galleryClient, IPackageProvider packageProvider)
{
    private readonly IGalleryClient _galleryClient = galleryClient;
    private readonly IPackageProvider _packageProvider = packageProvider;

    public Task<FunctionClosure> ResolveAsync(
        FunctionCatalog requested,
        IReadOnlyList<CatalogFunction> seeds,
        IncludeDependencies mode,
        CancellationToken cancellationToken)
    {
        var run = new Run(this, requested, mode);

        return run.ExecuteAsync(seeds, cancellationToken);
    }

    internal static string ToWireName(IncludeDependencies mode) => mode switch
    {
        IncludeDependencies.None => "none",
        IncludeDependencies.Package => "package",
        _ => "all",
    };

    /// <summary>The state of one resolution: the loaded packages are its cache.</summary>
    private sealed class Run(DependencyResolver resolver, FunctionCatalog requested, IncludeDependencies mode)
    {
        private readonly DependencyResolver _resolver = resolver;
        private readonly FunctionCatalog _requested = requested;
        private readonly IncludeDependencies _mode = mode;

        private readonly Dictionary<string, FunctionCatalog> _loaded = new(StringComparer.OrdinalIgnoreCase)
        {
            [requested.PackageId] = requested,
        };

        // Dependencies that cannot be loaded, with the warning that collects their callers.
        private readonly Dictionary<string, WarningBuilder> _unusable = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Entry> _included = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Entry> _inclusionOrder = [];
        private readonly Queue<Entry> _queue = new();
        private readonly List<(Entry Caller, FunctionCatalog Catalog, CatalogFunction Target)> _skippedLocalCalls = [];
        private readonly List<(Entry Caller, string Name)> _unmatchedCalls = [];
        private readonly Dictionary<(string Code, string? Package, string? Version, string? Function), WarningBuilder> _warnings = [];

        public async Task<FunctionClosure> ExecuteAsync(IReadOnlyList<CatalogFunction> seeds, CancellationToken cancellationToken)
        {
            foreach (var seed in seeds)
            {
                if (Include(_requested, seed, isRequested: true) is { } collision)
                    return FunctionClosure.Failed(collision);
            }

            while (_queue.TryDequeue(out var entry))
            {
                if (await VisitAsync(entry, cancellationToken) is { } error)
                    return FunctionClosure.Failed(error);
            }

            ReportSkippedLocalCalls();
            ReportUnmatchedCalls();
            ReportPackageCycles();

            if (FindCycle() is { } cycle)
                return FunctionClosure.Failed(new ClosureError.RecursiveCall(cycle));

            return FunctionClosure.Built(BuildPackages(), [.. _warnings.Values.Select(builder => builder.Build())]);
        }

        private async Task<ClosureError?> VisitAsync(Entry entry, CancellationToken cancellationToken)
        {
            var caller = entry.Catalog;

            foreach (var name in entry.Function.CallNames)
            {
                if (DaxBuiltInFunctions.Contains(name))
                    continue;

                if (caller.TryGetFunction(name, out var local))
                {
                    entry.Calls.Add(local.Name);

                    if (_mode is IncludeDependencies.None)
                    {
                        _skippedLocalCalls.Add((entry, caller, local));
                        continue;
                    }

                    if (Include(caller, local, isRequested: false) is { } collision)
                        return collision;

                    entry.Callees.Add(local.Name);
                    continue;
                }

                if (FindDeclaredDependency(caller, name) is { } dependency)
                {
                    var error = await VisitExternalCallAsync(entry, dependency, name, cancellationToken);

                    if (error is not null)
                        return error;

                    continue;
                }

                entry.Calls.Add(name);

                // A call without a dot is a keyword or a function this list does not know: only a
                // dotted name can be a function of a package or of the model.
                if (name.Contains('.', StringComparison.Ordinal))
                    _unmatchedCalls.Add((entry, name));
            }

            return null;
        }

        private async Task<ClosureError?> VisitExternalCallAsync(
            Entry entry,
            PackageDependency dependency,
            string name,
            CancellationToken cancellationToken)
        {
            var dependencyId = dependency.PackageId.ToNormalizedString();
            var declaredVersion = dependency.Version.ToNormalizedString();
            var caller = entry.Function.Name;

            if (_mode is not IncludeDependencies.All)
            {
                entry.Calls.Add(name);
                Warn(DefinitionWarningCodes.MissingDependency, dependencyId, declaredVersion, name,
                    $"{name} is not in the block because includeDependencies is '{ToWireName(_mode)}'. " +
                    $"It is in the dependency {dependencyId} {declaredVersion}. Use includeDependencies 'all' to add it.")
                    .AddCaller(caller);
                return null;
            }

            if (_unusable.TryGetValue(dependencyId, out var unusable))
            {
                entry.Calls.Add(name);
                unusable.AddCaller(caller);
                return null;
            }

            if (_loaded.TryGetValue(dependencyId, out var catalog))
            {
                if (!catalog.Version.Equals(dependency.Version))
                {
                    entry.Calls.Add(name);
                    var loadedVersion = catalog.Version.ToNormalizedString();
                    Warn(DefinitionWarningCodes.VersionConflict, dependencyId, loadedVersion, function: null,
                        $"Package {dependencyId} is required at more than one version. The block contains version {loadedVersion}; " +
                        "the calls that need another version are not satisfied. Only exact versions are used.")
                        .AddVersion(loadedVersion)
                        .AddVersion(declaredVersion)
                        .AddCaller(caller);
                    return null;
                }
            }
            else
            {
                var load = await LoadAsync(entry.Catalog, dependency, cancellationToken);

                if (load.Error is not null)
                    return load.Error;

                if (load.Catalog is null)
                {
                    entry.Calls.Add(name);
                    load.Warning!.AddCaller(caller);
                    return null;
                }

                catalog = load.Catalog;
            }

            if (!catalog.TryGetFunction(name, out var target))
            {
                entry.Calls.Add(name);
                Warn(DefinitionWarningCodes.FunctionNotFound, dependencyId, declaredVersion, name,
                    $"{name} is not in {dependencyId} {declaredVersion}, the version that the manifest of {entry.Catalog.PackageId} declares.")
                    .AddCaller(caller);
                return null;
            }

            entry.Calls.Add(target.Name);

            if (Include(catalog, target, isRequested: false) is { } collision)
                return collision;

            entry.Callees.Add(target.Name);
            return null;
        }

        private async Task<(FunctionCatalog? Catalog, WarningBuilder? Warning, ClosureError? Error)> LoadAsync(
            FunctionCatalog declaringPackage,
            PackageDependency dependency,
            CancellationToken cancellationToken)
        {
            var dependencyId = dependency.PackageId.ToNormalizedString();
            var declaredVersion = dependency.Version.ToNormalizedString();
            var versions = await _resolver._galleryClient.GetVersionsAsync(dependency.PackageId, cancellationToken);

            if (versions.Count == 0)
            {
                var notFound = Warn(DefinitionWarningCodes.DependencyNotFound, dependencyId, declaredVersion, function: null,
                    $"Package {dependencyId}, a dependency declared in the manifest of {declaringPackage.PackageId}, is not published. " +
                    "The calls into it are not satisfied.");
                _unusable[dependencyId] = notFound;
                return (null, notFound, null);
            }

            using var package = versions.Contains(dependency.Version)
                ? await _resolver._packageProvider.GetPackageAsync(dependency.PackageId, dependency.Version, cancellationToken)
                : null;

            if (package is null)
            {
                var published = versions.Select(version => version.ToNormalizedString()).ToArray();
                var versionNotFound = Warn(DefinitionWarningCodes.DependencyVersionNotFound, dependencyId, declaredVersion, function: null,
                    $"Version {declaredVersion} of {dependencyId}, declared in the manifest of {declaringPackage.PackageId}, is not published. " +
                    "Only the exact declared version is used, so the calls into it are not satisfied.")
                    .WithPublishedVersions(published);
                _unusable[dependencyId] = versionNotFound;
                return (null, versionNotFound, null);
            }

            FunctionCatalog catalog;

            try
            {
                catalog = FunctionCatalog.Create(package);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return (null, null, new ClosureError.TmdlParseFailed(dependencyId, declaredVersion, exception));
            }

            _loaded[dependencyId] = catalog;
            return (catalog, null, null);
        }

        /// <summary>
        /// Adds the function to the block, unless it is there. Returns an error when another
        /// package already added a function with the same name.
        /// </summary>
        private ClosureError.NameCollision? Include(FunctionCatalog catalog, CatalogFunction function, bool isRequested)
        {
            if (_included.TryGetValue(function.Name, out var existing))
            {
                if (ReferenceEquals(existing.Catalog, catalog))
                    return null;

                return new ClosureError.NameCollision(function.Name, [existing.Catalog.PackageId, catalog.PackageId]);
            }

            var entry = new Entry(catalog, function, isRequested);
            _included.Add(function.Name, entry);
            _inclusionOrder.Add(entry);
            _queue.Enqueue(entry);
            return null;
        }

        // With includeDependencies 'none' the local calls are only collected: a callee that is
        // also a requested function is in the block, every other one is missing.
        private void ReportSkippedLocalCalls()
        {
            foreach (var (caller, catalog, target) in _skippedLocalCalls)
            {
                if (_included.TryGetValue(target.Name, out var included))
                {
                    caller.Callees.Add(included.Function.Name);
                    continue;
                }

                Warn(DefinitionWarningCodes.MissingDependency, catalog.PackageId, catalog.Version.ToNormalizedString(), target.Name,
                    $"{target.Name} is not in the block because includeDependencies is 'none'. " +
                    "Use includeDependencies 'package' or 'all' to add it.")
                    .AddCaller(caller.Function.Name);
            }
        }

        // Classified after the closure, when every package that will be loaded is loaded.
        private void ReportUnmatchedCalls()
        {
            foreach (var (caller, name) in _unmatchedCalls)
            {
                var owner = _loaded.Values
                    .Where(catalog => !ReferenceEquals(catalog, caller.Catalog)
                        && name.StartsWith(catalog.PackageId + ".", StringComparison.OrdinalIgnoreCase))
                    .MaxBy(catalog => catalog.PackageId.Length);

                if (owner is not null)
                {
                    Warn(DefinitionWarningCodes.UndeclaredDependency, owner.PackageId, owner.Version.ToNormalizedString(), name,
                        $"{name} belongs to package {owner.PackageId}, which the manifest of {caller.Catalog.PackageId} does not declare. " +
                        "It was not added to the block.")
                        .AddCaller(caller.Function.Name);
                }
                else
                {
                    Warn(DefinitionWarningCodes.UnresolvedCall, package: null, version: null, name,
                        $"{name} is not a DAX built-in function and not a function of a loaded package. " +
                        "It can be a function of the model.")
                        .AddCaller(caller.Function.Name);
                }
            }
        }

        // Information only: the loaded map already stops the loop.
        private void ReportPackageCycles()
        {
            HashSet<string> reported = new(StringComparer.OrdinalIgnoreCase);

            foreach (var cycle in FindCycles(
                _loaded.Values.Select(catalog => catalog.PackageId),
                packageId => _loaded[packageId].Dependencies
                    .Select(dependency => dependency.PackageId.ToNormalizedString())
                    .Where(_loaded.ContainsKey)))
            {
                var key = string.Join('|', cycle.Skip(1).Order(StringComparer.OrdinalIgnoreCase));

                if (!reported.Add(key))
                    continue;

                Warn(DefinitionWarningCodes.PackageCycle, cycle[0], _loaded[cycle[0]].Version.ToNormalizedString(), function: null,
                    $"The package manifests declare a dependency cycle: {string.Join(" → ", cycle)}.");
            }
        }

        private IReadOnlyList<string>? FindCycle() =>
            FindCycles(_inclusionOrder.Select(entry => entry.Function.Name), name => _included[name].Callees)
                .FirstOrDefault();

        /// <summary>
        /// Depth-first search for cycles; each cycle lists its nodes with the first one repeated at
        /// the end. A self-edge is a cycle of one node.
        /// </summary>
        private static List<IReadOnlyList<string>> FindCycles(IEnumerable<string> nodes, Func<string, IEnumerable<string>> edges)
        {
            Dictionary<string, bool> visited = new(StringComparer.OrdinalIgnoreCase); // false: on the current path
            List<string> path = [];
            List<IReadOnlyList<string>> cycles = [];

            foreach (var node in nodes)
                Visit(node);

            return cycles;

            void Visit(string node)
            {
                if (visited.TryGetValue(node, out var done))
                {
                    if (!done)
                    {
                        var start = path.FindIndex(item => string.Equals(item, node, StringComparison.OrdinalIgnoreCase));
                        cycles.Add([.. path.Skip(start), path[start]]);
                    }

                    return;
                }

                visited[node] = false;
                path.Add(node);

                foreach (var next in edges(node))
                    Visit(next);

                path.RemoveAt(path.Count - 1);
                visited[node] = true;
            }
        }

        /// <summary>
        /// Dependency packages in post-order of the declared dependencies, the requested package
        /// last; inside a package, the catalog order.
        /// </summary>
        private List<ClosurePackage> BuildPackages()
        {
            List<FunctionCatalog> order = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            Visit(_requested);

            List<ClosurePackage> packages = [];

            foreach (var catalog in order)
            {
                ClosureFunction[] functions =
                [
                    .. catalog.Functions
                        .Where(function => _included.TryGetValue(function.Name, out var entry) && ReferenceEquals(entry.Catalog, catalog))
                        .Select(function => _included[function.Name].ToClosureFunction()),
                ];

                var isRequested = ReferenceEquals(catalog, _requested);

                if (functions.Length > 0 || isRequested)
                    packages.Add(new ClosurePackage(catalog, isRequested, functions));
            }

            return packages;

            void Visit(FunctionCatalog catalog)
            {
                if (!seen.Add(catalog.PackageId))
                    return;

                foreach (var dependency in catalog.Dependencies)
                {
                    if (_loaded.TryGetValue(dependency.PackageId.ToNormalizedString(), out var loaded))
                        Visit(loaded);
                }

                order.Add(catalog);
            }
        }

        private WarningBuilder Warn(string code, string? package, string? version, string? function, string message)
        {
            var key = (code, package, version, function);

            if (!_warnings.TryGetValue(key, out var builder))
            {
                builder = new WarningBuilder(code, message, package, version, function);
                _warnings.Add(key, builder);
            }

            return builder;
        }

        /// <summary>
        /// The declared dependency whose package id is the longest prefix of the name, if any.
        /// </summary>
        private static PackageDependency? FindDeclaredDependency(FunctionCatalog caller, string name) =>
            caller.Dependencies
                .Where(dependency => name.StartsWith(dependency.PackageId.ToNormalizedString() + ".", StringComparison.OrdinalIgnoreCase))
                .MaxBy(dependency => dependency.PackageId.ToNormalizedString().Length);
    }

    private sealed class Entry(FunctionCatalog catalog, CatalogFunction function, bool isRequested)
    {
        public FunctionCatalog Catalog { get; } = catalog;

        public CatalogFunction Function { get; } = function;

        public bool IsRequested { get; } = isRequested;

        /// <summary>Every non-built-in call, for the output.</summary>
        public List<string> Calls { get; } = [];

        /// <summary>The included functions this one calls, for the recursion check.</summary>
        public List<string> Callees { get; } = [];

        public ClosureFunction ToClosureFunction() =>
            new(Function, Catalog, IsRequested, [.. Calls.Distinct(StringComparer.OrdinalIgnoreCase)]);
    }

    private sealed class WarningBuilder(string code, string message, string? package, string? version, string? function)
    {
        private readonly List<string> _callers = [];
        private readonly List<string> _versions = [];
        private IReadOnlyList<string>? _publishedVersions;

        public WarningBuilder AddCaller(string caller)
        {
            if (!_callers.Contains(caller, StringComparer.OrdinalIgnoreCase))
                _callers.Add(caller);

            return this;
        }

        public WarningBuilder AddVersion(string version)
        {
            if (!_versions.Contains(version, StringComparer.OrdinalIgnoreCase))
                _versions.Add(version);

            return this;
        }

        public WarningBuilder WithPublishedVersions(IReadOnlyList<string> versions)
        {
            _publishedVersions = versions;
            return this;
        }

        public DefinitionWarning Build() => new(
            code,
            message,
            package,
            version,
            _versions.Count == 0 ? null : [.. _versions],
            _publishedVersions,
            function,
            [.. _callers]);
    }
}
