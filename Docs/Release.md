# Release workflow

`master` is the only long-lived branch. Feature work uses short-lived branches and pull requests targeting `master`; this repository does not create `release/*` branches.

## Channels

- Manual workflow run without `manual_version`: build a preview package and publish it to GitHub Packages.
- `vX.Y.Z-rc.N` tag reachable from `origin/master`: publish that prerelease to NuGet.org.
- `vX.Y.Z` tag reachable from `origin/master`: publish that stable version to NuGet.org.
- Manual workflow run with `manual_version`: publish to NuGet.org only when the workflow runs from `master`.

NuGet versions and Git tags are immutable. Query the official NuGet index before publishing and never move or recreate an existing tag.

## Release sequence

1. Fetch `origin`, start a short-lived branch from `origin/master`, and verify the worktree is clean.
2. Build the Swift wrappers, collect the locked Facebook xcframeworks, pack the NuGet and validate its module selection.
3. Open a PR to `master` and require CI to pass.
4. Merge and fetch the resulting `origin/master` commit.
5. Create the RC or stable tag on that exact commit and push only the new tag.
6. Verify the publish workflow and the resulting NuGet package before updating consumers.

Do not delete historical tags. Delete a temporary branch only after every retained commit is reachable from `origin/master`.
