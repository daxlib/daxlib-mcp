# MCP elicitation

**Decision:** no tool sends `elicitation/create`. A decision that belongs to the user is either left
to the client or provided explicitly as tool input.

Removed: `InstallConfirmation`, previously used by `install_package` to confirm the write operation.
This was the server's only use of elicitation.

## Why

- Claude Code advertises the `elicitation` capability in every tested session.
- In an interactive terminal session, it presents the request to the user.
- In a non-interactive session (print mode), including the mode used by the desktop application, it
  returns `cancel` immediately without presenting the request to the user.
- The server cannot distinguish that response from a user dismissing the prompt. The removed
  implementation therefore treated it as a user cancellation, causing `install_package` to fail
  even though the user had never been asked.
- This cannot be detected through capability negotiation:
  `ClientCapabilities.Elicitation` is non-null in both cases.

## Reference

Microsoft's Power BI Modeling MCP ([comparison](../design/02-powerbi-modeling-mcp.md)):

- Implements elicitation as opt-in. `SkipConfirmation` defaults to `true`;
  `--require-confirmation` enables it.
- Treats `decline` and `cancel` as equivalent negative outcomes, as the removed implementation did,
  and therefore has the same limitation when elicitation is enabled.

## Instead

- `install_package` is marked `Destructive = true` and `ReadOnly = false`, allowing the client's
  permission flow to obtain user approval before the call. Elicitation duplicated that confirmation.
- Existing constraints remain: the target must be an exact `dataSource` returned by
  `list_local_instances`, and only user-defined functions are written.

## Before reintroducing

- Distinguish a client-generated `cancel` from an explicit user decision.
- Provide a path that does not require elicitation.
- Verify behavior in interactive and non-interactive modes for every supported client.
