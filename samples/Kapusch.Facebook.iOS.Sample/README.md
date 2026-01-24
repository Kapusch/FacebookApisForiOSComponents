# Kapusch.Facebook.iOS.Sample

Small, buildable iOS sample project for validating:
- Managed API compilation
- Native asset injection via `NativeReference` (wrapper + Facebook xcframeworks)

No secrets are committed.

## Build (local)

Prereqs:
- macOS + Xcode
- .NET SDK 10 (`global.json` pins 10.0.100)

Build native assets (repo-only):
```bash
bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build.sh
bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/collect-facebook-xcframeworks.sh
```

Build the sample (simulator, no signing):
```bash
dotnet build samples/Kapusch.Facebook.iOS.Sample/Kapusch.Facebook.iOS.Sample.csproj \
  -c Debug \
  -p:RuntimeIdentifier=iossimulator-arm64 \
  -p:EnableCodeSigning=false
```

## Runtime notes

This sample calls into the interop API but does not include a configured Facebook App ID / URL schemes.
To make the login flow work end-to-end, update the generated `Info.plist` (gitignored) with the required Facebook keys and URL schemes.
