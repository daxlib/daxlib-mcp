# Changelog

Changes to the DAX Lib MCP server, newest release first.

## [0.1.0] - 2026-10-12

### Other

- First stable release, with the same features as 0.1.0-beta.4

## [0.1.0-beta.4] - 2026-08-28 - Pre-release

### Fixes

- `install_package` no longer fails when the client cannot display its confirmation prompt, and now
  relies on the client's own permission request

## [0.1.0-beta.1] - 2026-08-19 - Pre-release

### Features

- `search_packages` searches the DAX Lib gallery and returns matching packages with their latest
  version, download count, and description
- `get_package` returns a package's details, including authors, links, release notes, and every
  published version
- `get_package_readme` returns the README published with a package version
- `list_local_instances` lists the Power BI Desktop instances running on this machine
- `install_package` adds a package's user-defined DAX functions to a model open in Power BI Desktop,
  replacing any existing function with the same name
