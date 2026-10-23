# Dependency resolution for `get_function_definitions`

**Decision:** `get_function_definitions` returns a DEFINE block with the requested functions and the
functions they call (the call closure), in the same package and in the packages that the manifests
declare. It uses only the exact versions that the manifests declare, and it never renames library
code.

## Call closure

- Calls are found with a tokenizer (`QueryDefinitions/DaxTokenizer.cs`), not a regex. A call is an
  identifier, dots included, directly followed by `(`. Names inside strings and comments are not
  calls. On all 64 published packages (577 functions) a regex found 316 calls between functions of
  the same package and the tokenizer 300; the 16 extra regex calls were names inside strings or
  comments.
- A call is classified in this order: DAX built-in function (ignored; list from `INFO.FUNCTIONS`);
  function of the same package; function of a package that the caller's manifest declares (longest
  package id prefix); function of a loaded package that the caller does not declare
  (`undeclaredDependency`, not added); any other dotted name (`unresolvedCall`, possibly a model
  function).
- `includeDependencies`: `none` returns only the requested functions, `package` adds the callees
  in the same package, `all` (default) also adds the callees in the declared dependencies.
- A dependency package is downloaded only when a call goes into it, and once per tool call. Every
  published function name starts with its package id, so the target package is known from the
  name.
- The engine rejects recursion in user-defined functions, so a cycle in the closure is the error
  `recursiveCall`. Two included functions with the same name from different packages are the error
  `nameCollision`.

## Exact versions only

- A dependency is loaded at the version the caller's manifest declares. When that version, or the
  package, is not published, the tool reports `dependencyVersionNotFound` or `dependencyNotFound`
  and does not pick another version.
- When two callers need two versions of one package (a diamond), the first version loaded stays,
  and the tool reports `versionConflict`. The requested package always stays at the requested
  version.
- A package cycle in the manifests does not loop: each package is loaded once, and the tool reports
  `packageCycle` as information.

## Warnings and `complete`

The block is always returned. `complete` is false when a called function is not in the block:
`missingDependency`, `dependencyNotFound`, `dependencyVersionNotFound`, `versionConflict`,
`functionNotFound`. The engine accepts a DEFINE block with a missing callee, and the query fails
only when it calls the affected caller. `undeclaredDependency`, `unresolvedCall` and `packageCycle`
are information only.

## No renames

The conversion from TMDL is the minimum the engine needs: `DEFINE` before the first function, and
the name without quotes. The deserializer removes the fences and the annotations. Names and
expressions are returned as published.

Two published packages have a function name part that the current DAX parser rejects
(`DaxLib.SVG.Element.Group`, `Ankush.SemanticInfo.Model.Describe`). No quote style makes such a
name work, and a rename would make the block differ from the installed functions. The package
owners publish new names; the tool description tells the client that the query error points to the
name part.

## Why

- A query that changes nothing in the model is the safest way for an assistant to use a function.
- Version ranges, a fallback to the latest version and the highest version on a diamond need
  agreed rules, and the install path (DaxLib.Modeling) matches dependencies by exact version too.
  Exact versions keep the query and the install consistent.

## Future enhancements

Out of scope today, to be agreed with the DAX Lib maintainers:

- Fallback to the latest version when a declared version is not published, with a warning.
- The highest version on a diamond, with a warning.
- Version ranges in `manifest.daxlib` (proposal: NuGet rules, lowest matching version).
- Dependencies in the gallery `metadata.json`, so the tool can read them without a download.
