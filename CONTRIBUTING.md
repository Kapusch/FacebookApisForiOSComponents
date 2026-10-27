# Contributing

Thanks for contributing!

## Prerequisites

- macOS with Xcode installed (required for iOS SDK tooling)
- .NET SDK 10 (this repo pins `10.0.100` via `global.json`)

## Local build

If you are working without the NuGet (ProjectReference), see `Docs/SourceMode.md`.


Build the Objective-C wrappers (this also restores and collects the pinned Facebook SDK xcframeworks):

- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build.sh`

Re-collect the Facebook SDK xcframeworks only (restores them from `DependencyLocks/iOS/lockstate.txt` when needed):

- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/collect-facebook-xcframeworks.sh`

Pack the NuGet:

- `dotnet pack src/Kapusch.FacebookApisForiOSComponents/Kapusch.FacebookApisForiOSComponents.csproj -c Release -o artifacts/nuget`

## Formatting

- C#: follow `.editorconfig` (tabs, LF).
- Objective-C: keep changes minimal and consistent with existing style (tabs, ARC, Objective-C SDK surface only).

## Pull requests

- Keep PRs focused and well-scoped.
- Branch from `master` and target `master`; this repository does not use long-lived `release/*` branches.
- Do not commit secrets.
- If you update the Facebook SDK version, update every line of `DependencyLocks/iOS/lockstate.txt` together. The SHA-256 values must equal the `remoteChecksum` values of the upstream `Package.swift` at the same tag.

See `Docs/Release.md` before changing versions, tags, or publishing workflows.

## License

By contributing, you agree that your contributions will be licensed under the repository license (MIT).
