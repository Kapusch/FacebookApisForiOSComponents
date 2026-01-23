# FacebookApisForiOSComponents

Public OSS repository that packages **Facebook Login for iOS** into a consumable .NET NuGet.

## Package

- NuGet ID: `Kapusch.Facebook.iOS`

## What this repo ships

A NuGet package that:
- provides a small managed API for **Facebook Login (iOS)**, and
- redistributes the required **Facebook iOS SDK xcframeworks** inside the `.nupkg` (classic/native binding packaging),
- injects the xcframeworks into consuming apps via `buildTransitive` `NativeReference` items.

## Third-party licenses

See `THIRD_PARTY_NOTICES.md`.

## Developer docs

- Formatting: `Docs/Formatting.md`
- Source mode: `Docs/SourceMode.md`

## Build (local)

Prereqs:
- Xcode installed (for `xcrun`, iOS SDKs)
- .NET SDK 10 (`global.json` pins 10.0.100)

Build the native wrapper:
- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build.sh`

Collect Facebook xcframeworks for packing:
- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/collect-facebook-xcframeworks.sh`

Pack the NuGet:
- `dotnet pack src/Kapusch.FacebookApisForiOSComponents/Kapusch.FacebookApisForiOSComponents.csproj -c Release -o artifacts/nuget`

## Consumption

- Install the package from GitHub Packages (pre-release).
- Follow `Docs/Integration.md` for Info.plist keys and AppDelegate hooks.

## CI

- PR CI is build-only.
- Publishing is handled by a workflow that pushes a pre-release to GitHub Packages.
