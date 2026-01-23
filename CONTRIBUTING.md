# Contributing

Thanks for contributing!

## Prerequisites

- macOS with Xcode installed (required for iOS SDK tooling)
- .NET SDK 10 (this repo pins `10.0.100` via `global.json`)

## Local build

If you are working without the NuGet (ProjectReference), see `Docs/SourceMode.md`.


Build the Swift wrapper:

- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build.sh`

Collect the Facebook SDK xcframeworks (from the SwiftPM scratch produced by the build):

- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/collect-facebook-xcframeworks.sh`

Pack the NuGet:

- `dotnet pack src/Kapusch.FacebookApisForiOSComponents/Kapusch.FacebookApisForiOSComponents.csproj -c Release -o artifacts/nuget`

## Formatting

- C#: follow `.editorconfig` (tabs, LF).
- Swift: keep changes minimal and consistent with existing style.

## Pull requests

- Keep PRs focused and well-scoped.
- Do not commit secrets.
- If you update the Facebook SDK version, update `Package.swift` and `Package.resolved` together.

## License

By contributing, you agree that your contributions will be licensed under the repository license (MIT).
