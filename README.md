# DAX Lib MCP Server

Local [MCP](https://modelcontextprotocol.io/) server for [DAX Lib](https://daxlib.org), the public
gallery of reusable DAX user-defined function (UDF) packages.

It gives an MCP-compatible assistant — such as Claude, GitHub Copilot or Codex — three capabilities:

1. Search the DAX Lib catalog and inspect package metadata, versions and READMEs.
2. Get a package's DAX functions as a `DEFINE` block, to use them in a DAX query without changing
   the model.
3. Install a package's DAX functions into a semantic model open in Power BI Desktop.

## Requirements

| Requirement | Notes |
|---|---|
| Node.js | Version 18 or later. Used only to launch the server. |
| Power BI Desktop | Required to install packages. The target model must be at compatibility level 1702 or later. Not required to get a `DEFINE` block. |

## Supported platforms

The published package supports Windows x64 (`win-x64`) with all tools, including installation into
a model open in Power BI Desktop. Linux, macOS, Windows ARM64 and other platforms are not currently
published.

## Installation

Add the server to the MCP client configuration:

```json
{
  "mcpServers": {
    "daxlib-mcp": {
      "command": "npx",
      "args": ["-y", "@daxlib/mcp", "--start"]
    }
  }
}
```

VS Code and Visual Studio use `servers` instead of `mcpServers`. VS Code also supports
[one-click installation](https://vscode.dev/redirect/mcp/install?name=daxlib-mcp&config=%7B%22command%22%3A%22npx%22%2C%22args%22%3A%5B%22-y%22%2C%22%40daxlib%2Fmcp%22%2C%22--start%22%5D%7D).

No further setup is required. The server has no configuration file, no environment variables and
no API keys.

### Standalone executable

Each [release](https://github.com/daxlib/daxlib-mcp/releases) also provides the executable as a zip
archive. Extract it and register `daxlib-mcp.exe` with the `--start` argument in place of the `npx`
command shown above. Running `daxlib-mcp.exe` without arguments prints the configuration snippet
with the executable path already set.

## Usage

Example requests:

- "Search DAX Lib for packages that generate SVG images."
- "Show the README of DaxLib.SVG."
- "List the Power BI Desktop models currently open."
- "Install DaxLib.SVG into the Sales Report model."
- "Give me the DEFINE block for DaxLib.SVG.Viz.Pill."
- "Write a DAX query that uses DaxLib.FormatString to format the sales amount."

## Tools

| Tool | Description |
|---|---|
| `search_packages` | Searches the DAX Lib catalog by keyword, with paging and optional inclusion of prereleases. |
| `get_package` | Returns the description, authors, tags, links, release notes and published versions of a package. |
| `get_package_readme` | Returns the README published with a package version. |
| `get_function_definitions` | Returns a DAX query `DEFINE` block with the selected functions of a package and the functions they call. |
| `list_local_instances` | Lists the Power BI Desktop instances running on the local machine with their connection details. |
| `install_package` | Installs a DAX Lib package into a local Power BI Desktop model. |

## DAX Lib functions in a DAX query

`get_function_definitions` returns the functions of a package as a `DEFINE` block. Paste the block
before `EVALUATE` to use the functions in that query only; the model does not change. The block
contains the requested functions and the functions they call, also in the dependency packages that
the manifest declares, at the declared versions.

- It needs no Power BI Desktop instance and no installation, and it has no compatibility-level
  requirement: query-scoped functions depend on the engine, not on the model.
- Supported engines: the Power BI service (also through the XMLA endpoint), Azure Analysis Services,
  and Power BI Desktop from September 2025 (preview) and June 2026 (general availability).
- A function in the `DEFINE` block replaces a model function with the same name, for that query
  only.
- A few functions published before a change of the DAX parser cannot run in a query, because a part
  of their name is now reserved (for example `DaxLib.SVG.Element.Group`). The server returns them
  unchanged; the query error points to the name part.

## Command line

```text
daxlib-mcp [--start | --version | --help]
```

| Option | Description |
|---|---|
| `--start` | Starts the MCP server on standard input and output. This is the command used by MCP clients. |
| `--version` | Prints the version. |
| `--help`, `-h` | Prints the usage information. |

Running the executable without arguments prints an overview and the client configuration snippet.

## More information

Source code, issue tracking and release information are available at [github.com/daxlib/daxlib-mcp](https://github.com/daxlib/daxlib-mcp).
The [documentation index](docs/README.md) lists the available technical documentation.

## License

MIT, see [LICENSE](LICENSE). Packages published on DAX Lib are contributed by the community under their own licenses.
