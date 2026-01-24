# Kapusch.FacebookApisForiOSComponents — AI Working Agreement

## Goals
- Produce a reproducible iOS NuGet package for Facebook Login interop.
- Do not commit secrets.

## Packaging constraints
- Public OSS repo: keep docs/sample generic and not app-specific.
- The NuGet ships the required `xcframework`s and references them via `NativeReference`.
- Consuming apps must not download native deps at build time.
- The repo may use SwiftPM during CI/build to fetch the upstream Facebook iOS SDK, but consuming apps must not download native deps at build time.

## Repo layout
- `src/Kapusch.FacebookApisForiOSComponents/` — NuGet project (managed API + buildTransitive MSBuild)
- `src/Kapusch.FacebookApisForiOSComponents/Native/iOS/` — Swift wrapper source + scripts (repo-only)
- `Docs/` — integration docs
- `samples/` — optional sample template (no secrets committed)

## Safety
- Do not add new dependency ingestion paths without documenting them in `README.md`.
- Do not commit real app ids/secrets.
