# AGENTS.md

This file provides guidance for coding agents working with this repository. It is not end-user
product documentation.

## Overview

Local stdio MCP server for [DAX Lib](https://daxlib.org), the public gallery of DAX user-defined
function packages. It exposes the catalog (search, package details, README) and installs a
package's functions into a running Power BI Desktop semantic model through TOM.

The server is distributed exclusively through NPM as `@daxlib/mcp`, with the platform package
`@daxlib/mcp-win32-x64` carrying the self-contained executable.

## Documentation

The technical documentation index is [docs/README.md](docs/README.md).

## Commands

```bash
dotnet build DaxLib.Mcp.slnx
dotnet test --solution DaxLib.Mcp.slnx
```

`dotnet test` runs in the .NET 10 MTP mode (the `"test"` section in `global.json`). A single test
project can also be run directly with:

```bash
dotnet run --project tests/DaxLib.Mcp.Core.Tests
```

## .NET SDK and lock files

`global.json` pins the SDK to an exact version (`"rollForward": "disable"`), and CI restores in
locked mode. The SDK adds an implicit `Microsoft.NET.ILLink.Tasks` reference whose version follows
the runtime bundled with the SDK, so every SDK update changes `packages.lock.json`. Update the SDK
on purpose: change `global.json`, run `dotnet restore DaxLib.Mcp.slnx --force-evaluate` with that
SDK, and commit both together.

## Release process

Releases are launched manually with the `release.yml` workflow (`workflow_dispatch`), only from
`main`, never on tag push. [docs/release-process.md](docs/release-process.md) describes the
versioning rules, the preparation steps, and the changelog conventions. Follow it whenever you
prepare a release or write a changelog entry.
