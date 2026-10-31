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

Facebook Login reports only the tokens of the current login, never succeeds without a
token, runs LoginKit on the main thread from any calling thread, and releases a cancelled
login so a retry can start. For Firebase Limited Login, pass SHA-256 of your raw nonce to
`SignInAsync` and give Firebase the raw nonce: the returned `Nonce` is the token's claim.

App Events never enables anything on its own: ship the Info.plist switches off,
initialize after consent and see `Docs/Integration.md` for the consent recipe and
for avoiding double counting with server-side purchase events.

See the repo `README.md` and `Docs/Integration.md` for integration steps.
