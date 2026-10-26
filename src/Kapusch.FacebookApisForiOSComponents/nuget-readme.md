# Kapusch.Facebook.iOS

This package provides:
- managed APIs for Facebook Login and photo sharing on iOS, and
- the required Facebook iOS SDK `xcframework`s packaged for .NET iOS.

Set `KapuschFacebookFeatures` to `Login` (default), `Share`, or `Login;Share`.
Bundles Facebook iOS SDK 19.0.0 (iOS 15.0+). CoreKit, which every feature
uses, requires FBAEMKit.
Photo sharing uses ShareKit's native-app dialog, guarantees one managed
completion per request, and avoids reading the general pasteboard when cleaning
Meta's app-bridge marker on return.

See the repo `README.md` and `Docs/Integration.md` for integration steps.
