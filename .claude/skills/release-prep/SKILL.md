---
name: release-prep
description: Prepare the next release of the DAX Lib MCP server. Collects the changes since the last published version, agrees the version bump with the maintainer, and updates version.json and CHANGELOG.md, stopping at a local commit. Use when asked to prepare or cut a release, bump the version, write the changelog for the next version, or list what has changed since the last release.
---

# Release preparation

The procedure is in [docs/release-process.md](../../../docs/release-process.md). Read it first and
follow it exactly: it is the source of truth for the versioning rules, the preparation steps, and
the changelog conventions. This file only adds how to carry it out with the maintainer.

## How to work through it

Decide nothing the maintainer should decide, and stop before anything that leaves the machine.

- **Ask about the bump before writing anything.** Two questions, each with a proposed answer and a
  one-line reason drawn from the changes actually read: the semver step (major, minor, patch, or no
  bump) and the channel (keep the current prerelease tag, use a different one, or promote to
  stable).
- **Show the changelog entry and get approval before committing.** The wording is the deliverable
  and it is the maintainer's call. Present the proposed entry, apply any correction, then commit.
- **Never invent changelog lines.** Every line traces back to a diff that was read. When a commit's
  user impact cannot be determined from its diff, ask about that commit rather than describing it
  vaguely.
- **Touch only `version.json` and `CHANGELOG.md`.** The npm `package.json` files and everything
  under `npm/` belong to the release workflow.
- **Stop at the commit.** Do not push, tag, create a release, or run the release workflow. Report
  the released version, the changed files, the commit hash, and the publication steps that remain.

Reading the diffs of many commits is the one part worth delegating to a subagent, and only when the
range is large enough that the diffs would crowd out the conversation. The change list it returns
still gets reviewed here.

## Stop conditions

- No commits since the last release: say so and change nothing.
- Uncommitted changes in the working tree: stop. Do not stash, and do not commit work that belongs
  to someone else.
- Not on `main`: stop. Releases only come from `main`.
- The last released version cannot be located in history: report what was found and ask which commit
  is the release base.

## First run

`CHANGELOG.md` does not exist yet. Create it with a short neutral header and backfill the entries
for the versions already released, reconstructed from git history, before adding the new entry.
