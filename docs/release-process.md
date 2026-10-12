# Release process

A release has two phases. Preparation happens on a release branch and ends with a pull request
merged into `main`. Publication happens in the `release.yml` workflow, launched manually from the
GitHub Actions UI.

## Versioning

Versions come from [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning).
The `version.json` file at the repository root is the only version source, and the `nbgv` CLI reads
it. The npm `package.json` files stay at `0.0.0-placeholder`; the workflow sets their versions when
it stages the packages.

A version bump means editing the `version` field in `version.json`:

| Intent | `version` field | Released version |
|---|---|---|
| Prerelease | `X.Y.Z-beta.{height}` | `X.Y.Z-beta.1` |
| Stable | `X.Y.Z` | `X.Y.Z` |
| No bump | unchanged | current version with the height raised by one |

The `{height}` macro counts the commits since the `version` field last changed, so a commit that
edits `version.json` resets it to `1`, and every later commit raises it by one. The macro is valid
only inside the prerelease tag: `X.Y.{height}` makes every `nbgv` command fail.

Run `nbgv get-version -v NpmPackageVersion` to read the version of the working tree, or
`nbgv get-version <commit> -v NpmPackageVersion` for a specific commit.

Only `main` produces public versions (`publicReleaseRefSpec` in `version.json`). While any other
branch is checked out, `nbgv` appends the commit hash, as in `0.1.0-gf84aff39b7` or
`0.1.0-beta.1.g0959f23f22`; the version proper is everything before that suffix.

## Preparing a release

1. Confirm the working tree is clean, then create the release branch from an up-to-date
   `origin/main`, for example `alberto/release-<version>`.
2. Determine the last released version from the top entry of `CHANGELOG.md`, or from the highest
   `v*` tag when the file does not exist yet.
3. Resolve the commit that version was built from. Run `nbgv get-version <tag> -v NpmPackageVersion`
   on the matching tag and compare the result with the tag name. On a mismatch the tag is misplaced;
   walk `git rev-list --first-parent HEAD` and take the first commit whose version matches.
4. List the commits with `git log --first-parent <base>..HEAD` and read the diff of each one. The
   subject says what was touched, the diff says what changed for the person using the server.
   Disregard commits that only touch tests, CI, agent-facing documentation, or internal refactors.
5. Decide the semver step and the channel, then edit `version.json` as described above.
6. Add the entry to `CHANGELOG.md` at the repository root, following the conventions below.
7. Commit `version.json` and `CHANGELOG.md` together, as `chore(release): prepare <version>`. This
   must be the only commit on the release branch.
8. Verify that `nbgv get-version -v NpmPackageVersion`, without the commit hash suffix, matches the
   version in the new changelog heading. Correct the files and amend the commit if it does not.
9. Push the branch and open a pull request into `main`.
10. Merge the pull request with a squash merge, so `main` receives a single commit that edits
    `version.json`. A merge commit would add one to the height and turn `X.Y.Z-beta.1` into
    `X.Y.Z-beta.2`.
11. Update the local `main` and verify that `nbgv get-version -v NpmPackageVersion` on it matches
    the changelog heading exactly. On a mismatch, fix it with another pull request before
    publishing.

## Changelog conventions

`CHANGELOG.md` at the repository root is the single source. The workflow copies it into both npm
packages, so no changelog is committed under `npm/`.

The reader is a Power BI user working through an AI client, who never reads this repository.

- Describe what the release does for that reader. "Package installation no longer stops when the
  model already contains a function with the same name", not "fixed null handling in the installer".
- Omit type names, file paths, project names, namespaces, commit hashes, and pull request numbers.
- MCP tool names belong in backticks, because users see them in their client.
- One line per change, one sentence, sentence case, no trailing period.
- Group the lines under `### Features`, `### Fixes`, `### Security`, and `### Other`. Omit the
  sections with no lines.
- When a release contains nothing user-visible, write a single Fixes line: `Minor fixes and
  stability improvements`.

The newest entry comes first, directly under the file header. The heading carries the version, the
date of the preparation commit, and the release kind:

```text
## [0.2.0-beta.1] - 2026-08-28 - Pre-release
```

Stable releases drop the trailing ` - Pre-release`.

## Publishing

Once the release pull request is merged, launch the `release.yml` workflow from the Actions UI,
before any other pull request is merged into `main`. Choose the `next` dist-tag
for a prerelease and `latest` for a stable release; the workflow rejects `latest` for a version that
carries a prerelease tag.

The workflow builds and tests the solution, signs the Windows executable, stages and publishes both
npm packages, and creates the GitHub release as a draft. It generates the release notes from the
commits, so those notes do not match the changelog and are worth replacing before publishing the
draft.

The tag is created when the draft is published, and GitHub attaches it to whatever commit is at the
head of `main` at that moment. A tag can therefore name a version that was built from an earlier
commit, which is why step 3 above verifies tags instead of trusting them.
