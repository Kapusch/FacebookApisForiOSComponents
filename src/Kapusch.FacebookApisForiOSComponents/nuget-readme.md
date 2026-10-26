# Kapusch.Facebook.iOS

This package provides:
- managed APIs for Facebook Login, photo sharing and Meta App Events on iOS, and
- the required Facebook iOS SDK `xcframework`s packaged for .NET iOS.

Set `KapuschFacebookFeatures` to `Login` (default), `Share`, `AppEvents` or a
combination such as `Login;Share`. `FacebookAppEvents` works with every selection.
Bundles Facebook iOS SDK 18.0.2. CoreKit, which every feature uses,
requires FBAEMKit.
Photo sharing uses ShareKit's native-app dialog, guarantees one managed
completion per request, and avoids reading the general pasteboard when cleaning
Meta's app-bridge marker on return.

App Events never enables anything on its own: ship the Info.plist switches off,
initialize after consent and see `Docs/Integration.md` for the consent recipe and
for avoiding double counting with server-side purchase events.

See the repo `README.md` and `Docs/Integration.md` for integration steps.
