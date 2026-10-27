# Kapusch.FacebookApisForiOSComponents — AI Working Agreement

## Goals
- Produce a reproducible iOS NuGet package for selectable Facebook Login and Share interop.
- Do not commit secrets.

## Packaging constraints
- Public OSS repo: keep docs/sample generic and not app-specific.
- The NuGet ships the required `xcframework`s and references them via `NativeReference`.
- Consuming apps must not download native deps at build time.
- The repo build downloads the upstream Facebook iOS SDK release xcframeworks pinned (URL + SHA-256) in `DependencyLocks/iOS/lockstate.txt`; consuming apps must not download native deps at build time.
- The native facades are Objective-C and use only the SDK's Objective-C headers, so they do not depend on the Swift compiler release Meta used for the binaries.

## Repo layout
- `src/Kapusch.FacebookApisForiOSComponents/` — NuGet project (managed API + buildTransitive MSBuild)
- `src/Kapusch.FacebookApisForiOSComponents/Native/iOS/` — Objective-C wrapper source + scripts (repo-only)
- `DependencyLocks/iOS/lockstate.txt` — pinned Facebook iOS SDK artifacts
- `Docs/` — integration docs
- `samples/` — optional sample template (no secrets committed)

## Validation
- `bash tests/validate-feature-selection.sh` — feature selection and wrapper source invariants.
- `python3 scripts/validate-package.py` — packed layout, native exports, App Events bindings and constants.
- `python3 scripts/verify-public.py` — no real Facebook credentials or emails in tracked files, and no private term read from `KAPUSCH_PRIVATE_TERMS` or the untracked `.git/info/private-terms`.
- `python3 scripts/verify-app-symbols.py <app executable>` — a built app (CI builds the sample) keeps every `kfb_*` entry point.
- When package contents or native entry points change, update `scripts/validate-package.py` in the same commit.

## Safety
- Do not add new dependency ingestion paths without documenting them in `README.md`.
- Do not commit real app ids/secrets.

## Branches and releases
- `master` is the only long-lived branch and the source of every NuGet.org release.
- Implement changes on short-lived branches and target `master` through a PR. Never implement directly on `release/*`.
- Both stable (`vX.Y.Z`) and prerelease (`vX.Y.Z-rc.N`) tags must reference commits reachable from `origin/master`.
- Manual runs without a version publish previews to GitHub Packages. See `Docs/Release.md` for the complete contract.
