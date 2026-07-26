# Integration (iOS)

This document applies whether you consume the package via NuGet or via ProjectReference (see `Docs/SourceMode.md`).

## 1) Info.plist requirements

Your iOS app must include:

- `FacebookDisplayName`
- `FacebookAppID`
- `FacebookClientToken`

And a `CFBundleURLTypes` entry with the URL scheme:

- `fb<FACEBOOK_APP_ID>`

Additionally, for app-to-app flows (Facebook app), include these query schemes:

- `fbapi`
- `fb`
- `fb-messenger-share-api`
- `fb-app-share`

## 2) AppDelegate hooks

Call the interop hooks in your `AppDelegate`:

- On launch: `NativeFacebookLogin.Initialize(app, options)`
- On URL open: `NativeFacebookLogin.HandleOpenUrl(app, url, options)`

These hooks apply to `KapuschFacebookFeatures=Login`. For Share-only builds,
disable Meta automatic initialization and call
`NativeFacebookShare.ConfigureAndInitialize(app.Handle, trackingAllowed)` only
after the ATT decision, immediately before the first share. Then call
`NativeFacebookShare.SharePhotoAsync(...)`; no caption is accepted.
Forward Facebook callback URLs to
`NativeFacebookShare.HandleOpenUrl(app.Handle, url.Handle, options.Handle)`.
Share-only host applications should filter by their configured Facebook URL
scheme before invoking this handler.

The photo wrapper uses ShareKit's native-app dialog mode. A share operation
completes its managed callback at most once, including when ShareKit reports a
synchronous presentation failure followed by a late delegate callback.

When Facebook returns to the host app, the wrapper clears only Meta's private
bridge marker from the general pasteboard, and does so without reading the
pasteboard. This avoids triggering an iOS paste-authorization prompt while
leaving unrelated pasteboard content untouched.

## 3) Feature selection

- `Login` (default): Login wrapper and LoginKit.
- `Share`: photo-share wrapper and ShareKit.
- `Login;Share`: both managed/native entry points.

At SDK 18.0.2, ShareKit's CoreKit dependency requires FBAEMKit. Treat this as a
privacy-review blocker if the consumer's artifact policy excludes FBAEMKit.

## 4) Limited Login

For Limited Login, pass:
- `FacebookTrackingMode.Limited`
- a non-empty `nonce` (raw nonce string)

The result can contain:
- `AuthenticationToken`
- `Nonce`

## 5) Secrets policy

Do not commit real values in this repo.
Use templates and `.gitignore`d local files for any sample app configuration.
