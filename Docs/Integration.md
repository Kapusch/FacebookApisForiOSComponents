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

At SDK 18.0.2, CoreKit (required by every feature, App Events included) links
FBAEMKit. Treat this as a privacy-review blocker if the consumer's artifact policy
excludes FBAEMKit.

## 4) App Events (attribution and conversion events)

`Kapusch.Facebook.iOS.FacebookAppEvents` wraps `FBSDKAppEvents`, `FBSDKSettings` and
`FBSDKApplicationDelegate` of the bundled Facebook iOS SDK 18.0.2. Event and parameter
names are available as constants in `FacebookAppEventNames`,
`FacebookAppEventParameterNames`, `FacebookAppEventParameterValues` and
`FacebookAppEventUserDataTypes`. The API mirrors `Kapusch.Facebook.Android` (section 4.8).

### 4.1 Info.plist keys

Besides the keys of section 1 (`FacebookAppID`, `FacebookClientToken`,
`FacebookDisplayName`, `CFBundleURLTypes` with `fb<FACEBOOK_APP_ID>`), the SDK reads
these booleans. Values set at run time through `FacebookAppEvents` are persisted by the
SDK and override the `Info.plist` values on later launches.

| Key | SDK default | Runtime API | Controls |
|---|---|---|---|
| `FacebookAutoLogAppEventsEnabled` | `true` | `SetAutoLogAppEventsEnabled` | Automatic activation / install events and, if enabled for the app at Meta, automatic in-app purchase events. See the server override below. |
| `FacebookAdvertiserIDCollectionEnabled` | `true` | `SetAdvertiserIdCollectionEnabled` | IDFA collection (all zeros until App Tracking Transparency is authorized) |
| `FacebookSKAdNetworkReportEnabled` | `true` | `SetSKAdNetworkReportEnabled` | SKAdNetwork conversion-value updates by the SDK |

**Server override (SDK 18.0.2).** Once the SDK has downloaded the app's configuration
from Meta (after initialization; the configuration is cached across launches), an
automatic-logging value set for the app on Meta's side (App Dashboard / Events
Manager) takes precedence over `FacebookAutoLogAppEventsEnabled` and
`SetAutoLogAppEventsEnabled`, in both directions. Facebook iOS SDK 19 changed this so a
local `false` always wins, but this package bundles 18.0.2. Turn automatic logging off
at Meta as well when the app must control it, and read `IsAutoLogAppEventsEnabled`,
which returns the effective value.

The metadata switches of SDK 19 (`FBSDKAutoLogMetaDataEnabled`,
`FBSDKAutoLogProductImprovementEnabled`) do not exist in 18.0.2, which does not
collect that data. Android's `com.facebook.sdk.AutoInitEnabled` has no iOS counterpart
either: on iOS the SDK only starts when the app initializes it (section 4.2).

If the app requests App Tracking Transparency, add `NSUserTrackingUsageDescription`.
The privacy manifests of `FBSDKCoreKit`, `FBSDKLoginKit` and `FBSDKShareKit` declare
`NSPrivacyTracking=true` with the tracking domain `ep1.facebook.com`; reflect it in your
App Store privacy answers. iOS blocks that domain until tracking is authorized, and the
SDK then routes app events to Meta's non-tracking endpoint.

### 4.2 Initialization and consent gating

The SDK sends nothing before it is initialized, but initialization itself contacts Meta
(SDK and app configuration requests) even when automatic logging is off. It happens when
the app calls `FacebookAppEvents.InitializeSdk(...)` or `FinishedLaunching(...)`,
`NativeFacebookLogin.Initialize(...)` or `NativeFacebookShare.ConfigureAndInitialize(...)`;
`FacebookAppEvents.IsSdkInitialized` then returns `true`.

Until then, the SDK would silently drop events, user IDs and user data, and report
default setting values (caching some of them for the whole process). `FacebookAppEvents`
therefore throws `InvalidOperationException` from `ActivateApp`, the `Log*` methods,
`Flush`, the user ID / user data methods and the `Is…Enabled` getters before
initialization, and `HandleOpenUrl` / `ContinueUserActivity` return `false` without
forwarding. The `Set…Enabled` setters, `SetDataProcessingOptions`, `SetFlushBehavior`,
`SetDebugLoggingEnabled`, `GetAnonymousId` and `SdkVersion` work at any time: values set
before initialization apply immediately and are applied again once the SDK is
initialized, so the SDK persists them.

A consent-first integration therefore:

1. Ships with the switches off in `Info.plist`:

   ```xml
   <key>FacebookAutoLogAppEventsEnabled</key><false/>
   <key>FacebookAdvertiserIDCollectionEnabled</key><false/>
   ```

   and turns automatic logging off at Meta too (server override, section 4.1).
2. Does not initialize the SDK before the user consents. Login and Share initialize it
   as well, so with those features the settings above are what keeps app events off.
3. After consent (and after the ATT prompt, if the app shows one):

   ```csharp
   FacebookAppEvents.InitializeSdk(); // or InitializeSdk([FacebookDataProcessingOptions.LimitedDataUse])
   FacebookAppEvents.SetAdvertiserIdCollectionEnabled(true);
   // Keep automatic logging off when a server reports purchases (section 4.4):
   FacebookAppEvents.SetAutoLogAppEventsEnabled(false);
   FacebookAppEvents.ActivateApp(); // activation + one-time install ping
   ```

   Then call `FacebookAppEvents.ActivateApp()` each time the app becomes active
   (`OnActivated`), because automatic activation logging is off. The SDK only logs a new
   activation when the app was inactive for longer than Meta's session timeout (60
   seconds by default).
4. On withdrawal, while the SDK is initialized:

   ```csharp
   FacebookAppEvents.SetFlushBehavior(FacebookAppEventsFlushBehavior.ExplicitOnly);
   FacebookAppEvents.SetAutoLogAppEventsEnabled(false);
   FacebookAppEvents.SetAdvertiserIdCollectionEnabled(false);
   FacebookAppEvents.ClearUserId();
   FacebookAppEvents.ClearUserData();
   // and stop calling ActivateApp / Log* / Flush.
   ```

   What stops at once:
   - activation and event logging by the app (on iOS the SDK only logs an activation when
     `ActivateApp` runs or when automatic logging is on);
   - IDFA collection, the user ID and the advanced-matching user data, including for
     events still queued;
   - sending queued events: with `ExplicitOnly`, the SDK no longer flushes on its
     15-second timer or when the app becomes active, and keeps them on the device.

   What only stops later:
   - automatic logging is checked at each activation, but a value set at Meta overrides
     the local `false` (section 4.1); StoreKit transaction observation for automatic
     purchase logging only stops when the SDK reloads its configuration, the next time
     the app becomes active;
   - the SDK stays initialized, and keeps its timers and configuration requests, until
     the process ends; there is no shutdown API;
   - the flush behaviour is not persisted. On the next launches, do not initialize the
     SDK. If Login or Share initializes it anyway, call
     `SetFlushBehavior(FacebookAppEventsFlushBehavior.ExplicitOnly)` before them, or the
     events queued before the withdrawal are sent at the first activation.

   If the SDK is not initialized in the current process, nothing is sent and the identity
   methods throw: data the SDK stored on the device stays there, unused, until the SDK is
   initialized again. The SDK cannot delete events already sent; that goes through
   Meta's data deletion process.
5. On later launches, initialize only if consent is still granted.

For Limited Data Use, pass the options to `InitializeSdk`
(`InitializeSdk([FacebookDataProcessingOptions.LimitedDataUse])`, country/state 0 lets
Meta geolocate the user) so they also apply to the initialization requests, or call
`SetDataProcessingOptions(...)` at any time; an empty array disables it.

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
way. Both return `false` without forwarding until the SDK is initialized, so nothing is
recorded before consent. When consent is already known at launch, call
`FacebookAppEvents.FinishedLaunching(app.Handle, launchOptions?.Handle ?? IntPtr.Zero)`
from `FinishedLaunching` instead of `InitializeSdk()`, so a cold-start URL reaches the
SDK. With `Login` selected, `NativeFacebookLogin.HandleOpenUrl` forwards to the same
SDK entry point (whatever the App Events consent); call only one of them per URL.

### 4.4 Avoid double counting with server-side events

Meta does not deduplicate SDK events against server-side (Conversions API) events
unless both share an `event_id`, which the SDK does not expose. Use exactly one source
per event:

- When a server (for example a subscription platform integration) sends trial,
  subscription or purchase events to Meta, do **not** call `LogStartTrial`,
  `LogSubscribe` or `LogPurchase` for them, and disable automatic in-app purchase
  logging:
  - keep automatic logging off (`FacebookAutoLogAppEventsEnabled=false` and at Meta,
    section 4.1) and call `ActivateApp()` yourself: iOS only observes StoreKit
    transactions when automatic logging is effectively on; and/or
  - set "Log In-App Events Automatically" to "No" in the Meta App Dashboard.
- With SDK 18, automatic logging on iOS 15+ can observe StoreKit 2 transactions at each
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

### 4.7 Event parameters

`LogEvent` and the typed helpers accept `string`, `bool` (sent as `"1"` / `"0"`),
integral, finite `float` / `double` and `decimal` values. `decimal` and `ulong` values
are sent as exact text, like on Android. Event and parameter names must match Meta's
rules (2-40 characters: letters, digits, `_`, `-` or spaces); the SDK drops an event
with an invalid name or parameter and only logs it (enable `SetDebugLoggingEnabled`).

### 4.8 API parity with Kapusch.Facebook.Android

Same names and signatures on both platforms: `IsSdkInitialized`, `SetAutoLogAppEventsEnabled` /
`IsAutoLogAppEventsEnabled`, `SetAdvertiserIdCollectionEnabled` /
`IsAdvertiserIdCollectionEnabled`, `SetDataProcessingOptions`, `SetDebugLoggingEnabled`,
`SetFlushBehavior` / `FlushBehavior`, `LogEvent`, `LogPurchase`, `LogStartTrial`,
`LogSubscribe`, `LogCompleteRegistration`, `Flush`, `GetUserId` / `SetUserId` /
`ClearUserId`, `SetUserData` / `ClearUserData`, `FacebookAppEventsUserData`,
`FacebookAppEventsFlushBehavior`, `FacebookDataProcessingOptions` and the shared
constants.

| Android | iOS | Why |
|---|---|---|
| `InitializeSdk(context, options, country, state)`, `ActivateApp(context)`, `GetAnonymousId(context)` | Same without `context` | No Android context on iOS |
| `SetAutoInitEnabled`, `IsAutoInitEnabled`, `IsSdkFullyInitialized` | None | The iOS SDK has no auto-initialization |
| `SetMetadataCollectionEnabled` / `IsMetadataCollectionEnabled`, `SetAutoLogProductImprovementEnabled` / `IsAutoLogProductImprovementEnabled` | None | SDK 19 switches; the bundled iOS SDK 18.0.2 has neither the switches nor that data collection |
| None | `FinishedLaunching`, `HandleOpenUrl`, `ContinueUserActivity` | iOS app-delegate forwarding for AEM and deep links |
| None | `SetAdvertiserTrackingEnabled` / `IsAdvertiserTrackingEnabled` | iOS-only flag, ignored by the SDK on iOS 17+ (it reads the ATT status) and on earlier versions once Meta's domain configuration enables ATT handling |
| None | `SetSKAdNetworkReportEnabled` / `IsSKAdNetworkReportEnabled` | SKAdNetwork is iOS-only |
| None | `SetUserDataValue` / `ClearUserDataValue`, `SdkVersion` | iOS SDK single-field user data API; diagnostics |

Behaviour differences:

- Before initialization, the Android setters throw; the iOS setters are recorded and
  applied again once the SDK is initialized.
- App opens with automatic logging off: on iOS, `ActivateApp()` logs
  `fb_mobile_activate_app` as an explicit event. On Android (facebook-core 19.0.0),
  activations are automatic events, so `ActivateApp()` then sends only the install ping;
  measuring opens there needs `SetAutoLogAppEventsEnabled(true)`.
- After withdrawal, turning automatic logging off on Android stops activation events at
  once, while activity tracking keeps running until the process ends; the iOS SDK only
  logs activations when `ActivateApp` runs or automatic logging is on.
- On iOS, the automatic-logging value set at Meta overrides the local one (SDK 18.0.2,
  section 4.1).

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
