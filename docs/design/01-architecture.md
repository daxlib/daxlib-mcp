# Architecture

This document orients a developer or coding agent making changes to the repository. It describes
the request flows and records the invariants a change must preserve.

## System overview

`daxlib-mcp` is a local, stdio-based MCP server. It exposes the [DAX Lib](https://daxlib.org)
catalog, returns a package's user-defined DAX functions as the DEFINE block of a DAX query, and
installs them into a semantic model open in Power BI Desktop.

```text
MCP client
   │ stdio / JSON-RPC
   ▼
daxlib-mcp
   ├─ DAX Lib client ── search, metadata, README and package download
   ├─ TOM/TMDL parser ── package documents to an in-memory model
   ├─ Query definitions ── call closure and DEFINE block, no model connection
   └─ Power BI discovery + TOM ── select the requested instance and save functions
```

Two projects: `src/DaxLib.Mcp` is the console host, `src/DaxLib.Mcp.Core` contains everything
else. `src/Dax.Tokenizer` is a third-party DAX lexer, vendored unchanged until it ships as a NuGet
package. Tests live in `tests/DaxLib.Mcp.Core.Tests`.

## Request flows

Every tool wraps its body in `ToolCallInvocation.RunAsync`, which provides uniform error handling
and local diagnostics, and returns a `CallToolResult` produced by a renderer. Typed records stay
inside the implementation; the wire result is concise natural language.

`install_package` executes these steps in order, stopping at the first failure:

1. Locate the instance matching the caller's `dataSource`; reject when it is not found or is not
   Power BI Desktop.
2. Resolve id + optional version to an exact package (`PackageResolver`); an unknown id can return
   search suggestions instead.
3. Download the package (`PackageProvider`).
4. Parse its TMDL documents into a detached TOM model (`PackageTmdlSerializer`).
5. Connect to the instance (`PackageInstaller`), verify exactly one loaded database at
   compatibility level 1702 or later, clone each function into the target model replacing
   same-name functions, and save with one `Model.SaveChanges()` call.

After the save, the result warns (`dependencyNotInstalled`) about each dependency in the package
manifest whose package id owns no function of the model (`DAXLIB_PackageId` annotation).

`get_function_definitions` executes these steps, and never connects to a model:

1. Resolve id + optional version (`PackageResolver`), download the package (`PackageProvider`).
2. Read its TMDL into a `FunctionCatalog`: expression, description, parameters and calls of each
   function. Calls are found with the DAX lexer of `Dax.Tokenizer` (`src/Dax.Tokenizer`, a
   vendored copy of the Vpax-Obfuscator tokenizer), so names inside strings and comments are not
   calls.
3. Check the requested function names (`unknownFunction` with close names).
4. Build the call closure (`DependencyResolver`): callees in the same package, and in the declared
   dependency packages at the exact declared versions. Dependencies are downloaded only when a call
   goes into them. Problems become warnings, recursion is an error.
5. Write the DEFINE block (`DaxQueryWriter`) and return it as an embedded resource
   (`daxlib-define://<id>/<version>/define.dax`) next to the JSON envelope.

The rules are recorded in [../decisions/dependency-resolution.md](../decisions/dependency-resolution.md).

The connection is built from the discovered instance, never from the caller-supplied `dataSource`
string.

Discovery works without any Power BI API: it finds `msmdsrv` processes, maps their listening TCP
ports, and inspects the parent process to identify Power BI Desktop models. The returned
`dataSource` (`host:port`) is the stable install identifier.

## Invariants

A change must preserve these properties:

- **Standard output carries only MCP JSON-RPC.** Every log line goes to standard error.
- **Tool names are a published contract** (`ToolNames`). Renaming a tool is a breaking change.
- **Writes go only to an explicitly identified target.** `install_package` requires the exact
  `dataSource` from `list_local_instances` and connects through the discovered instance, not the
  caller's input.
- **Only user-defined functions are written.** A same-name function is replaced; no other model
  object is changed.
- **The server never saves the PBIX file.** Persisting the change is a user action in Power BI
  Desktop.
- **Package source never passes through the assistant's tokens.** Download, parsing and
  installation stay inside the server process, so functions are installed with full fidelity
  (formatting and comments preserved).
- **Compatibility level 1702 is verified before any modification.**
- **`get_function_definitions` never connects to a model** and has no compatibility-level check:
  query-scoped functions depend on the engine.
- **Library code is never renamed.** The DEFINE block differs from the TMDL only in the syntax the
  query needs (no quotes around the name, no fences, no annotations).
- **Dependencies use exact versions only**, the versions the manifests declare.
- **The user chooses.** The server instructions (`McpServerInfo.Instructions`) direct the client
  to let the user pick the package and the target instance, never automatically.
- **Installation from NPM is complete at `npm install`.** No `postinstall` scripts, no runtime
  downloads.

The write tool is marked destructive and idempotent in its MCP metadata; the catalog tools and
`get_function_definitions` are read-only. The server does not confirm the write itself: approval belongs to the client's
permission flow, for the reasons recorded in
[../decisions/mcp-elicitation.md](../decisions/mcp-elicitation.md).

## Platform and distribution

The published runtime identifier is `win-x64`. Power BI Desktop and the Windows APIs used by
discovery make the discovery and installation path Windows-only; the catalog tools and
`get_function_definitions` only require network access to DAX Lib. CI also builds and tests on
Linux and macOS (`linux-x64` and `osx-arm64` are declared runtime identifiers) as a try that does
not fail the run; nothing is published for those platforms.

NPM is the distribution channel:

- `@daxlib/mcp` is a small Node.js launcher that passes standard input and output directly to the
  executable.
- `@daxlib/mcp-win32-x64` is an optional, platform-gated dependency containing the self-contained
  single-file executable (no .NET installation required).

Versions are stamped in lockstep from Nerdbank.GitVersioning by the release workflow. The workflow
is manual (`workflow_dispatch`): it builds and tests the solution, publishes the Windows
executable and stages both NPM packages as release artifacts.
