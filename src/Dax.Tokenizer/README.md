# Dax.Tokenizer (vendored)

This folder is a copy of the DAX tokenizer of
[sql-bi/Vpax-Obfuscator](https://github.com/sql-bi/Vpax-Obfuscator/tree/main/src/Dax.Tokenizer),
an ANTLR-generated DAX lexer. `get_function_definitions` uses it to find the calls between
functions and to read the parameter lists.

| | |
|---|---|
| Source | `src/Dax.Tokenizer` of sql-bi/Vpax-Obfuscator |
| Commit | `8990d0b2fc2c2c3276ae73679e76f8c16b67f7be` |
| License | MIT, Copyright (c) 2024 SQLBI ([LICENSE.md](LICENSE.md)) |
| Runtime dependency | `Antlr4.Runtime.Standard` 4.13.1 (BSD-3-Clause, see [THIRD-PARTY-NOTICES.md](../../THIRD-PARTY-NOTICES.md)) |

## Rules for this folder

- The `.cs` files are unchanged from the source commit. Do not edit them here: fix the upstream
  repository, then copy the files again and update the commit above.
- `Dax.Tokenizer.csproj` is local: it keeps the upstream target framework and language settings
  and turns off the analyzers of this repository. The upstream `version.json` is not copied.

## Replacing it with the NuGet package

The tokenizer will be published on NuGet with the same `Dax.Tokenizer` namespace. Then:

1. Delete this folder and remove the project from `DaxLib.Mcp.slnx`.
2. In `src/DaxLib.Mcp.Core/DaxLib.Mcp.Core.csproj`, replace the `ProjectReference` to this project
   with a `PackageReference` to the package.
3. Run `dotnet restore DaxLib.Mcp.slnx --force-evaluate` and commit the lock files.
4. Keep the ANTLR entry in `THIRD-PARTY-NOTICES.md` while the package depends on ANTLR, and update
   its version when it changes.
