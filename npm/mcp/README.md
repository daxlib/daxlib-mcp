# @daxlib/mcp

NPM launcher for the local MCP server for [DAX Lib](https://daxlib.org). It lets an MCP-compatible
assistant search DAX function packages and install them into a model open in Power BI Desktop.

This is the main package and does not contain the executable directly. Its optional dependency
`@daxlib/mcp-win32-x64` contains the self-contained binary; npm selects the platform package from
the operating system and architecture.

## Requirements

| Requirement | Notes |
|---|---|
| Node.js | Version 18 or later. Used only to launch the server. |
| Platform | Windows x64, the only platform currently published. |
| Power BI Desktop | Required to install packages. The target model must be at compatibility level 1702 or later. |

The binary is self-contained and does not require a .NET installation. The package has no
`postinstall` script and does not download the binary after installation.

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

## Usage

Example requests:

- "Search DAX Lib for packages that generate SVG images."
- "Show the README of DaxLib.SVG."
- "List the Power BI Desktop models currently open."
- "Install DaxLib.SVG into the Sales Report model."

## Tools

| Tool | Description |
|---|---|
| `search_packages` | Searches the DAX Lib catalog by keyword, with paging and optional inclusion of prereleases. |
| `get_package` | Returns the description, authors, tags, links, release notes and published versions of a package. |
| `get_package_readme` | Returns the README published with a package version. |
| `list_local_instances` | Lists the Power BI Desktop instances running on the local machine with their connection details. |
| `install_package` | Installs a DAX Lib package into a local Power BI Desktop model. |

## Command line

```text
npx -y @daxlib/mcp [--start | --version | --help]
```

| Option | Description |
|---|---|
| `--start` | Starts the MCP server on standard input and output. This is the command used by MCP clients. |
| `--version` | Prints the version. |
| `--help`, `-h` | Prints the usage information. |

Without arguments, the launcher displays an overview and an MCP configuration snippet. MCP clients
start the server with `--start`.

## Links

- Source code, issues and releases: [github.com/daxlib/daxlib-mcp](https://github.com/daxlib/daxlib-mcp)
- Package gallery: [daxlib.org](https://daxlib.org)

## License

MIT, see [LICENSE](https://github.com/daxlib/daxlib-mcp/blob/main/LICENSE). Packages published on
DAX Lib are contributed by the community under their own licenses.
