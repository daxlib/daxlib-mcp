# Power BI Modeling MCP

Microsoft's **Power BI Modeling MCP** (`powerbi-modeling-mcp`) is used here as a reference
implementation for comparing architecture and distribution choices with daxlib-mcp. It is a local
stdio MCP server distributed on NPM as `@microsoft/powerbi-modeling-mcp`. It is written in C# on the
same `ModelContextProtocol` SDK and, like daxlib-mcp, modifies a live Power BI Desktop semantic
model through the Tabular Object Model, including DAX user-defined functions—the only write
operation daxlib-mcp performs. This overlap makes it a useful basis for comparison.

Background and usage information:
[Power BI Modeling MCP README](https://github.com/microsoft/powerbi-modeling-mcp/blob/main/README.md).

## NPM distribution

As of version `0.5.0-beta.12`, the NPM distribution is composed of one platform-independent
launcher package,
[`@microsoft/powerbi-modeling-mcp`](https://www.npmjs.com/package/@microsoft/powerbi-modeling-mcp),
and five platform packages declared by the launcher as `optionalDependencies`:

| Package | Operating system | CPU |
| --- | --- | --- |
| `@microsoft/powerbi-modeling-mcp-darwin-arm64` | macOS | ARM64 |
| `@microsoft/powerbi-modeling-mcp-linux-arm64` | Linux | ARM64 |
| `@microsoft/powerbi-modeling-mcp-linux-x64` | Linux | x64 |
| `@microsoft/powerbi-modeling-mcp-win32-arm64` | Windows | ARM64 |
| `@microsoft/powerbi-modeling-mcp-win32-x64` | Windows | x64 |

Each platform package sets its NPM `os` and `cpu` fields. Consequently, installing the launcher
also installs only the optional package matching the current machine. In NPM terminology,
64-bit Windows is `win32-x64`: `win32` identifies the Windows operating-system family, not a
32-bit executable.

For example, after running the following command on Windows x64:

```powershell
npm install @microsoft/powerbi-modeling-mcp
```

the two downloaded packages have this relevant structure under `node_modules`:

```text
node_modules\
  @microsoft\
    powerbi-modeling-mcp\
      CHANGELOG.md
      LICENSE
      README.md
      index.js
      package.json
    powerbi-modeling-mcp-win32-x64\
      dist\
        AnalysisServices.AppSettings.json
        powerbi-modeling-mcp.exe
      CHANGELOG.md
      LICENSE
      README.md
      index.js
      package.json
```

The launcher package's `index.js` is exposed as the `powerbi-modeling-mcp` command. The Windows
x64 package exposes `dist\powerbi-modeling-mcp.exe` as `powerbi-modeling-mcp-win32-x64`, while
`AnalysisServices.AppSettings.json` supplies its Analysis Services configuration. NPM also creates
the corresponding command shims in `node_modules\.bin`; those shims are generated during
installation and are not files contained in either package archive.

## Executable and application assemblies

`powerbi-modeling-mcp.exe` is a self-contained .NET single-file bundle. It is a native AppHost with
the .NET runtime and managed files appended in the bundle format, so the executable itself is not a
managed assembly and does not require a separately installed .NET runtime.

The bundle contains two application assemblies:

- `powerbi-modeling-mcp.dll`: the MCP server host.
- `PowerBIModelingMCP.Library.dll`: the modeling implementation library.

It also embeds `powerbi-modeling-mcp.deps.json`, which pins the MCP SDK and Tabular Object Model
dependencies. These managed files are inside `powerbi-modeling-mcp.exe`; they therefore do not
appear as separate files in the NPM package tree shown above.

## Decisions shared with the reference

- **NPM distribution model.** A thin Node.js wrapper whose `optionalDependencies` resolve to a
  platform-specific package containing a self-contained executable, with standard input and output
  handed over untouched. Platform selection is delegated entirely to npm through `os` and `cpu`
  constraints.
- **Write safety.** A write operation is never applied to an instance the caller did not
  explicitly identify.

## Decisions taken differently

- **Tool surface.** `powerbi-modeling-mcp` exposes roughly 200 operations through 22
  `*_operations` tools selected via an `Operation` discriminator, supported by additional `Help`
  and `ToolMetadata` tools. For daxlib-mcp's much smaller surface, five separate tools keep the
  schemas flat and the results self-describing; an operation router would complicate both the
  schemas and the wire protocol without benefit at this scale.
- **Runtime package installation.** The reference's NPM launcher can install the platform package
  at runtime. daxlib-mcp ships the binary inside the platform package instead, so an install is
  complete when `npm install` finishes, with no `postinstall` script and no download on first run.
