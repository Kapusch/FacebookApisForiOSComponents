# Kapusch.Facebook.iOS.Sample

Small, buildable iOS sample project for validating:
- Managed API compilation (Login and App Events; the sample selects `Login;AppEvents`)
- Native asset injection via `NativeReference` (wrapper + Facebook xcframeworks)
- The App Events consent recipe of `Docs/Integration.md` (grant / log / flush / withdraw buttons)

No secrets are committed.

## Build (local)

Prereqs:
- macOS + Xcode
- .NET SDK 10 (`global.json` pins 10.0.100)

Build native assets (repo-only; restores the pinned Facebook SDK archives):
```bash
bash src/Kapusch.FacebookApisForiOSComponents/Native/iOS/build.sh
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
To make the login flow or App Events work end-to-end, update the generated `Info.plist` (gitignored)
with `FacebookAppID`, `FacebookClientToken`, `FacebookDisplayName` and the `fb<APP_ID>` URL scheme.
Never commit those values.

The template ships the App Events switches off. "App Events: grant consent" initializes the SDK,
enables advertiser ID collection and SDK debug logging, and logs an activation; check the device log
and Events Manager > Test Events. "App Events: withdraw consent" follows the withdrawal steps of
`Docs/Integration.md` (explicit-only flushing, switches off, user ID and user data cleared). Without
a `FacebookAppID` the App Events buttons only print a message.

The sample also calls `NativeFacebookLogin.Initialize` at launch for its Login button, which
initializes the SDK before any App Events consent; a consent-first app initializes it only after
consent (`Docs/Integration.md`, section 4.2).
