# FacebookApisForiOSComponents

Public OSS repository that packages selected Facebook iOS features into a consumable .NET NuGet.

## Package

- NuGet ID: `Kapusch.Facebook.iOS`

## What this repo ships

A NuGet package that:
- provides managed APIs for **Facebook Login**, **Facebook photo sharing** and **Meta App Events**
  (install / activation attribution, standard and custom events, consent-related settings,
  AEM deep-link forwarding), and
- redistributes the required **Facebook iOS SDK xcframeworks** inside the `.nupkg` (classic/native binding packaging),
- injects the xcframeworks into consuming apps via `buildTransitive` `NativeReference` items.

Select features with `KapuschFacebookFeatures=Login`, `Share`, `AppEvents` or a combination
such as `Login;Share`. The default remains `Login` for compatibility. `FacebookAppEvents` is
available with every selection because CoreKit is always linked; `AppEvents` alone ships
CoreKit, AEMKit and CoreKit_Basics only.

Facebook iOS SDK 18.0.2 links `FBSDKCoreKit` to `FBAEMKit` non-weakly. Therefore
`Share` currently includes `ShareKit`, `CoreKit`, `CoreKit_Basics` and `FBAEMKit`.
A consumer that forbids `FBAEMKit` must block Store delivery; this repository
does not strip or conceal the dependency.

## Upstream dependency ingestion

- The repo build downloads Meta's prebuilt **dynamic** xcframework archives from the
  `facebook/facebook-ios-sdk` GitHub release named in `DependencyLocks/iOS/lockstate.txt`.
- Each archive is verified against the SHA-256 in that lock file (the same value as the
  upstream `Package.swift` `remoteChecksum`) before it is extracted.
- The wrappers are Objective-C and compile against the SDK's Objective-C headers only.
  They do not need the Swift compiler release Meta used to build the binaries.
- Consuming apps never download anything: the NuGet already contains every xcframework.

## Third-party licenses

See `THIRD_PARTY_NOTICES.md`.

## Developer docs

- Formatting: `Docs/Formatting.md`
- Source mode: `Docs/SourceMode.md`
- Release workflow: `Docs/Release.md`
- Samples: `samples/README.md`

## Build (local)

Prereqs:
- Xcode installed (for `xcrun`, iOS SDKs)
- .NET SDK 10 (`global.json` pins 10.0.100)

Build the native wrappers (restores the pinned SDK archives first):
- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build.sh`

Collect Facebook xcframeworks for packing:
- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/collect-facebook-xcframeworks.sh`

Pack the NuGet:
- `dotnet pack src/Kapusch.FacebookApisForiOSComponents/Kapusch.FacebookApisForiOSComponents.csproj -c Release -o artifacts/nuget`

## Consumption

- Install the package from NuGet.org for public release tags.
- Install the package from GitHub Packages for internal preview builds.
- Follow `Docs/Integration.md` for Info.plist keys, AppDelegate hooks, App Events consent
  gating and how to avoid double counting with server-side purchase events.

## CI

- PR CI is build-only.
- Publishing is handled by `.github/workflows/publish.yml` with channel routing:
	- tag `vX.Y.Z` on `master` -> NuGet.org (stable)
	- tag `vX.Y.Z-rc.N` on `master` -> NuGet.org (pre-release)
	- non-tag runs (`workflow_dispatch`) -> GitHub Packages (`X.Y.Z-preview.<run>.<sha>`)
	- `workflow_dispatch` on `master` with `manual_version` -> NuGet.org (forced version)
- NuGet.org publishing uses NuGet Trusted Publishing (OIDC via `NuGet/login@v1`), no long-lived NuGet API key.

### Required GitHub secret

- `NUGET_USER`: your nuget.org profile username (not email), used by `NuGet/login@v1`.

See `Docs/Release.md` for the validated release sequence. Historical tags remain immutable even when obsolete branches are deleted.
