# Third-Party Notices

This repository and its NuGet package (`Kapusch.Facebook.iOS`) redistribute third-party binaries.

## Facebook iOS SDK

- Upstream: facebook-ios-sdk
- Source: https://github.com/facebook/facebook-ios-sdk

The NuGet package includes Facebook SDK `xcframework`s under `fb/`, taken unmodified (apart
from removing the Mac Catalyst slice, signatures, dSYMs and Swift module files) from the
release archives pinned in `DependencyLocks/iOS/lockstate.txt`.

The privacy manifests of `FBSDKCoreKit`, `FBSDKLoginKit` and `FBSDKShareKit` declare
`NSPrivacyTracking=true` and the tracking domain `ep1.facebook.com`. Apps that ship this
package must reflect it in their own privacy declarations.
Each included `xcframework` contains an upstream `LICENSE` file.

If you are redistributing this package further, ensure you comply with the upstream license terms.
