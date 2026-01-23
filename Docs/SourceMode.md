# Source mode (ProjectReference)

This repo is primarily consumed as a NuGet package (`Kapusch.Facebook.iOS`).

Sometimes you may want to debug/iterate using a project reference instead.

## Option A (recommended): use NuGet

Use GitHub Packages pre-release and consume the package.

## Option B: ProjectReference (source mode)

1) Clone this repo (or add it as a git submodule) next to your app.

2) Build native assets (required for iOS runtime behavior):

- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build.sh`
- `bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/collect-facebook-xcframeworks.sh`

This produces:
- `src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build/kfb.xcframework`
- `src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build/fb/*.xcframework`

3) Add a project reference from your app to:

- `src/Kapusch.FacebookApisForiOSComponents/Kapusch.FacebookApisForiOSComponents.csproj`

4) If you build the repo in a different layout, you can set MSBuild properties on the consuming project:

- `InteropIosWrapperDir` (path to `kfb.xcframework`)
- `FacebookIosFrameworksDir` (path to the folder containing `fb/*.xcframework`)

Notes:
- Source mode is intended for local iteration; publishing should use the NuGet workflows.
