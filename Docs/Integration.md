# Integration (iOS)

This document applies whether you consume the package via NuGet or via ProjectReference (see `Docs/SourceMode.md`).

## 1) Info.plist requirements

The bundled Facebook iOS SDK 19.0.0 requires iOS 15.0 or later
(`SupportedOSPlatformVersion` >= 15.0).

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
after the ATT decision, immediately before the first share.
`ConfigureAndInitialize` sets automatic app events logging to off and advertiser
ID collection / advertiser tracking to `trackingAllowed`, except for any of these
settings the app already set through `FacebookAppEvents`, which it leaves as is. Then call
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

Set `KapuschFacebookFeatures` in the consuming project (semicolon-separated,
case-sensitive; unknown values produce a build warning):

- `Login` (default): Login wrapper and LoginKit.
- `Share`: photo-share wrapper and ShareKit.
- `Login;Share`: both managed/native entry points.
- `AppEvents`: App Events only. Ships CoreKit, AEMKit and CoreKit_Basics, without
  LoginKit or ShareKit.
- Any combination, for example `Login;AppEvents`.

`FacebookAppEvents` works with every selection: App Events lives in CoreKit, which
every feature links, so its small facade (`kfbappevents.xcframework`) is always
linked. Selecting `AppEvents` matters when you want neither Login nor Share.

When `Share` (or `Login`) is not selected, the package links a small
Foundation-only placeholder (`kfbsharestub.xcframework` / `kfbloginstub.xcframework`)
for its entry points, because the .NET iOS build always asks the native linker for
them. Calling `NativeFacebookShare.SharePhotoAsync(...)` then completes with
`Failed` / `share_feature_not_selected` and `NativeFacebookLogin.SignInAsync(...)`
with `Failed` / `login_feature_not_selected`; no ShareKit or LoginKit code is shipped.

At SDK 19.0.0, CoreKit (required by every feature) links FBAEMKit. Treat this as a
privacy-review blocker if the consumer's artifact policy excludes FBAEMKit.

## 4) App Events (attribution and conversion events)

`Kapusch.Facebook.iOS.FacebookAppEvents` wraps `FBSDKAppEvents`, `FBSDKSettings` and
`FBSDKApplicationDelegate`. Event and parameter names are available as constants in
`FacebookAppEventNames`, `FacebookAppEventParameterNames`,
`FacebookAppEventParameterValues` and `FacebookAppEventUserDataTypes`.

### 4.1 Info.plist keys

Besides the keys of section 1 (`FacebookAppID`, `FacebookClientToken`,
`FacebookDisplayName`, `CFBundleURLTypes` with `fb<FACEBOOK_APP_ID>`), the SDK reads
these booleans. Values set at run time through `FacebookAppEvents` are persisted by the
SDK and override the `Info.plist` values on later launches.

| Key | SDK default | Runtime API | Controls |
|---|---|---|---|
| `FacebookAutoLogAppEventsEnabled` | `true` | `SetAutoLogAppEventsEnabled` | Automatic activation / install events and, if enabled in the Meta dashboard, automatic in-app purchase events |
| `FacebookAdvertiserIDCollectionEnabled` | `true` | `SetAdvertiserIdCollectionEnabled` | IDFA collection (all zeros until App Tracking Transparency is authorized) |
| `FBSDKAutoLogMetaDataEnabled` | `true` | `SetMetadataCollectionEnabled` | SDK 19 metadata collection: screen titles and App Link URLs attached to events |
| `FBSDKAutoLogProductImprovementEnabled` | `true` | `SetAutoLogProductImprovementEnabled` | SDK 19 product-improvement switch (SDK 19.0.0 adds the control only) |
| `FacebookSKAdNetworkReportEnabled` | `true` | `SetSKAdNetworkReportEnabled` | SKAdNetwork conversion-value updates by the SDK |

Android's `com.facebook.sdk.AutoInitEnabled` has no iOS counterpart: on iOS the SDK only
starts when the app initializes it (section 4.2).

If the app requests App Tracking Transparency, add `NSUserTrackingUsageDescription`.
`FBSDKCoreKit`'s privacy manifest declares `NSPrivacyTracking=true` with the tracking
domain `ep1.facebook.com`; reflect it in your App Store privacy answers. iOS blocks that
domain until tracking is authorized, and the SDK then routes app events to Meta's
non-tracking endpoint.

### 4.2 Consent gating

The SDK sends nothing before it is initialized, but initialization itself contacts Meta
(SDK configuration requests) even when automatic logging is off. A consent-first
integration therefore:

1. Ships with every switch off in `Info.plist`:

   ```xml
   <key>FacebookAutoLogAppEventsEnabled</key><false/>
   <key>FacebookAdvertiserIDCollectionEnabled</key><false/>
   <key>FBSDKAutoLogMetaDataEnabled</key><false/>
   <key>FBSDKAutoLogProductImprovementEnabled</key><false/>
   ```

2. Does not call `FacebookAppEvents.InitializeSdk()` (nor
   `NativeFacebookLogin.Initialize` / `NativeFacebookShare.ConfigureAndInitialize`)
   before the user consents. Login and Share also initialize the SDK, so with those
   features the settings above are what keeps app events off.
3. After consent (and after the ATT prompt, if the app shows one):

   ```csharp
   FacebookAppEvents.InitializeSdk();
   FacebookAppEvents.SetAdvertiserIdCollectionEnabled(true);
   // Keep automatic logging off when a server reports purchases (section 4.4):
   FacebookAppEvents.SetAutoLogAppEventsEnabled(false);
   FacebookAppEvents.ActivateApp(); // activation + one-time install ping
   ```

   Then call `FacebookAppEvents.ActivateApp()` each time the app becomes active
   (`OnActivated`), because automatic activation logging is off. The SDK ignores
   activations less than 60 seconds apart.
4. On withdrawal:

   ```csharp
   FacebookAppEvents.SetAutoLogAppEventsEnabled(false);
   FacebookAppEvents.SetAdvertiserIdCollectionEnabled(false);
   FacebookAppEvents.ClearUserId();
   FacebookAppEvents.ClearUserData();
   // and stop calling ActivateApp / Log* methods.
   ```

   The SDK cannot delete events already sent; that goes through Meta's data deletion
   process.
5. On later launches, initialize only if consent is still granted.

Settings set before `InitializeSdk()` apply immediately and are applied again once the
SDK is initialized, so the SDK persists them in both cases.

For Limited Data Use, call
`FacebookAppEvents.SetDataProcessingOptions([FacebookDataProcessingOptions.LimitedDataUse])`
(country/state 0 lets Meta geolocate the user); an empty array disables it.

### 4.3 Deep links and Aggregated Event Measurement

Forward URLs and Universal Links so AEM can attribute iOS users who did not authorize
tracking:

```csharp
public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options) =>
	FacebookAppEvents.HandleOpenUrl(app.Handle, url.Handle, options?.Handle ?? IntPtr.Zero);

public override bool ContinueUserActivity(
	UIApplication app, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler) =>
	FacebookAppEvents.ContinueUserActivity(app.Handle, userActivity.Handle);
```

With a scene delegate, forward `OpenUrlContexts` and `ContinueUserActivity` the same
way. When consent is already known at launch, call
`FacebookAppEvents.FinishedLaunching(app.Handle, launchOptions?.Handle ?? IntPtr.Zero)`
from `FinishedLaunching` instead of `InitializeSdk()`, so a cold-start URL reaches the
SDK. With `Login` selected, `NativeFacebookLogin.HandleOpenUrl` forwards to the same
SDK entry point; call only one of them per URL.

### 4.4 Avoid double counting with server-side events

Meta does not deduplicate SDK events against server-side (Conversions API) events
unless both share an `event_id`, which the SDK does not expose. Use exactly one source
per event:

- When a server (for example a subscription platform integration) sends trial,
  subscription or purchase events to Meta, do **not** call `LogStartTrial`,
  `LogSubscribe` or `LogPurchase` for them, and disable automatic in-app purchase
  logging:
  - keep `FacebookAutoLogAppEventsEnabled=false` and call `ActivateApp()` yourself
    (iOS only observes StoreKit transactions when automatic logging is on); and/or
  - set "Log In-App Events Automatically" to "No" in the Meta App Dashboard.
- Since SDK 18, automatic logging on iOS scans StoreKit 2 transactions at each
  activation, renewals included, so leaving it on next to a server integration
  double-counts subscriptions.

### 4.5 SKAdNetwork

Only one component may update SKAdNetwork conversion values. If another SDK or your
own code owns them, call `FacebookAppEvents.SetSKAdNetworkReportEnabled(false)` (or set
`FacebookSKAdNetworkReportEnabled=false`). An advertised app does not need
`SKAdNetworkItems`; those are for apps that display ads.

### 4.6 Diagnostics

`FacebookAppEvents.SetDebugLoggingEnabled(true)` enables the SDK's app-events,
developer-error and informational logs in the device log; `false` restores the
previous logging behaviours. Combine it with Events Manager > Test Events.
`FacebookAppEvents.SdkVersion` returns the bundled SDK version.

### 4.7 Platform gaps

- No `SetAutoInitEnabled` on iOS (see 4.1).
- `SetAdvertiserTrackingEnabled` only matters on iOS 15 and 16; iOS 17+ uses the ATT
  status.
- `SetUserDataValue(FacebookAppEventUserDataTypes.ExternalId, ...)` is an iOS SDK API.

## 5) Limited Login

For Limited Login, pass:
- `FacebookTrackingMode.Limited`
- a non-empty `nonce` (raw nonce string)

The result can contain:
- `AuthenticationToken`
- `Nonce`

## 6) Secrets policy

Do not commit real values in this repo.
Use templates and `.gitignore`d local files for any sample app configuration.
