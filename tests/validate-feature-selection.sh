#!/usr/bin/env bash

set -euo pipefail

project="tests/FeatureSelection.proj"
share_source="src/Kapusch.FacebookApisForiOSComponents/Facebook/NativeFacebookShare.iOS.cs"
share_interop_source="src/Kapusch.FacebookApisForiOSComponents/Native/iOS/KapuschFacebookAuthInterop/Sources/KapuschFacebookShareInterop/Interop.m"

contains_fixed_text() {
	local pattern="$1"
	local file="$2"

	if command -v rg >/dev/null 2>&1; then
		rg -F "$pattern" "$file" >/dev/null
	else
		grep -Fq -- "$pattern" "$file"
	fi
}

login_source="src/Kapusch.FacebookApisForiOSComponents/Facebook/NativeFacebookLogin.iOS.cs"
if contains_fixed_text "NativeLibrary.GetExport" "$login_source"; then
	echo "Facebook Login must use static __Internal imports so iOS retains its native symbols." >&2
	exit 1
fi

for symbol in \
	kfb_facebook_initialize \
	kfb_facebook_handle_open_url \
	kfb_facebook_signin_start \
	kfb_facebook_signin_cancel \
	kfb_facebook_signout; do
	contains_fixed_text "EntryPoint = \"$symbol\"" "$login_source" \
		|| { echo "Missing static Facebook Login import: $symbol" >&2; exit 1; }
done

# Facebook Login callback contract.
login_interop_source="src/Kapusch.FacebookApisForiOSComponents/Native/iOS/KapuschFacebookAuthInterop/Sources/KapuschFacebookAuthInterop/Interop.m"
login_stub_source="src/Kapusch.FacebookApisForiOSComponents/Native/iOS/KapuschFacebookAuthInterop/Sources/KapuschFacebookLoginStub/Interop.m"
if contains_fixed_text "GCHandle" "$login_source"; then
	echo "Facebook Login must identify requests by a process-unique id, not a GCHandle that can be reused once freed." >&2
	exit 1
fi
contains_fixed_text "NativeFacebookSignInRequests.AttachCancellation(" "$login_source" \
	|| { echo "Facebook Login cancellation must go through the native wrapper so it releases its in-flight state." >&2; exit 1; }
for global_token in "currentAuthenticationToken.tokenString;" "currentAuthenticationToken.nonce" "currentAccessToken.tokenString;"; do
	if contains_fixed_text "$global_token" "$login_interop_source"; then
		echo "Facebook Login must report the tokens of its own LoginKit result, never the SDK's current tokens ($global_token)." >&2
		exit 1
	fi
done
for required in \
	"result.authenticationToken;" \
	"if (accessToken == nil && authenticationToken == nil) {" \
	"@\"missing_token\"" \
	"@\"nonce_mismatch\"" \
	"[nonce isEqualToString:request.expectedNonce]" \
	"if (!KFBFinishSignInRequest(request)) {" \
	"KFBStartSignIn(presenting, trackingMode, nonce, callback, context);" \
	"KFBAppEventsFinishLaunching(application, launchOptions, &available);"; do
	contains_fixed_text "$required" "$login_interop_source" \
		|| { echo "Facebook Login wrapper lost a callback-contract invariant: $required" >&2; exit 1; }
done
# Start, cancel, sign-out and initialization must all reach the main thread.
for entry_point in kfb_facebook_initialize kfb_facebook_signin_start kfb_facebook_signin_cancel kfb_facebook_signout; do
	awk -v name="$entry_point" '
		$0 ~ "^KFB_EXPORT .*" name "\\(" { inside = 1 }
		inside && /KFBRunOnMainThread\(|dispatch_async\(dispatch_get_main_queue\(\)/ { found = 1 }
		inside && /^}/ { exit }
		END { exit found ? 0 : 1 }
	' "$login_interop_source" \
		|| { echo "$entry_point must hop to the main thread." >&2; exit 1; }
done
if contains_fixed_text "didFinishLaunchingWithOptions" "$login_interop_source"; then
	echo "Facebook Login must initialize the SDK through the App Events initialization (single path)." >&2
	exit 1
fi
contains_fixed_text "KFB_EXPORT void kfb_facebook_signin_cancel(void *context)" "$login_stub_source" \
	|| { echo "The Login placeholder must define kfb_facebook_signin_cancel." >&2; exit 1; }

if contains_fixed_text "NativeLibrary.GetExport" "$share_source"; then
	echo "Facebook Share must use static __Internal imports so iOS retains its native symbols." >&2
	exit 1
fi

for symbol in \
	kfb_facebook_share_configure_and_initialize \
	kfb_facebook_share_handle_open_url \
	kfb_facebook_share_photo; do
	contains_fixed_text "EntryPoint = \"$symbol\"" "$share_source" \
		|| { echo "Missing static Facebook Share import: $symbol" >&2; exit 1; }
done

contains_fixed_text "if (_didFinish) {" "$share_interop_source" \
	|| { echo "Facebook Share callbacks must be idempotent." >&2; exit 1; }
contains_fixed_text "if (!shown && !delegate.didFinish)" "$share_interop_source" \
	|| { echo "Facebook Share must not invoke a second callback after ShareKit reports a synchronous failure." >&2; exit 1; }
contains_fixed_text "dialog.mode = FBSDKShareDialogModeNative;" "$share_interop_source" \
	|| { echo "Facebook photo share must use the native Facebook dialog instead of the deprecated iOS share sheet." >&2; exit 1; }
contains_fixed_text "KFBClearPendingShareDataWithoutReading();" "$share_interop_source" \
	|| { echo "Facebook native share callbacks must clear bridge data without triggering an iOS paste read." >&2; exit 1; }
for setting in KFBRegistrySettingAutoLogAppEvents KFBRegistrySettingAdvertiserIDCollection KFBRegistrySettingAdvertiserTracking; do
	contains_fixed_text "KFBExplicitAppEventsSetting($setting) == nil" "$share_interop_source" \
		|| { echo "Facebook Share must not override the App Events setting $setting configured by the app." >&2; exit 1; }
done
contains_fixed_text "KFBNotifyAppEventsSdkInitialized();" "$share_interop_source" \
	|| { echo "$share_interop_source must report SDK initialization to FacebookAppEvents." >&2; exit 1; }
for pasteboard_read in "dataForPasteboardType:" "valueForPasteboardType:"; do
	if contains_fixed_text "$pasteboard_read" "$share_interop_source"; then
		echo "Facebook Share wrapper must never read the general pasteboard." >&2
		exit 1
	fi
done

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=False

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Share -p:ExpectedLoginEnabled=False -p:ExpectedShareEnabled=True

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Login%3BShare -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=True

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=AppEvents -p:ExpectedLoginEnabled=False -p:ExpectedShareEnabled=False -p:ExpectedAppEventsEnabled=True

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Login%3BShare%3BAppEvents -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=True -p:ExpectedAppEventsEnabled=True

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=False -p:ExpectedAppEventsEnabled=False
