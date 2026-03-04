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
- Samples: `samples/README.md`

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

- Install the package from NuGet.org for public release tags.
- Install the package from GitHub Packages for internal preview builds.
- Follow `Docs/Integration.md` for Info.plist keys and AppDelegate hooks.

## CI

- PR CI is build-only.
- Publishing is handled by `.github/workflows/publish.yml` with channel routing:
	- tag `vX.Y.Z` on `master` -> NuGet.org (stable)
	- tag `vX.Y.Z-rc.N` on `release/*` -> NuGet.org (pre-release)
	- non-tag runs (`workflow_dispatch`) -> GitHub Packages (`X.Y.Z-preview.<run>.<sha>`)
	- `workflow_dispatch` with `manual_version` -> NuGet.org (forced version)
- NuGet.org publishing uses NuGet Trusted Publishing (OIDC via `NuGet/login@v1`), no long-lived NuGet API key.

### Required GitHub secret

- `NUGET_USER`: your nuget.org profile username (not email), used by `NuGet/login@v1`.

## Release examples

- Pre-release candidate from a release branch:
	- `git checkout release/1.0.0`
	- `git tag v1.0.0-rc.1`
	- `git push origin v1.0.0-rc.1`
- Stable release from master:
	- `git checkout master`
	- `git tag v1.0.0`
	- `git push origin v1.0.0`
