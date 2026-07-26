#!/usr/bin/env bash

set -euo pipefail

project="tests/FeatureSelection.proj"
share_source="src/Kapusch.FacebookApisForiOSComponents/Facebook/NativeFacebookShare.iOS.cs"
share_interop_source="src/Kapusch.FacebookApisForiOSComponents/Native/iOS/KapuschFacebookAuthInterop/Sources/KapuschFacebookShareInterop/Interop.swift"

contains_fixed_text() {
	local pattern="$1"
	local file="$2"

	if command -v rg >/dev/null 2>&1; then
		rg -F "$pattern" "$file" >/dev/null
	else
		grep -Fq -- "$pattern" "$file"
	fi
}

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

contains_fixed_text "guard !didFinish else { return }" "$share_interop_source" \
	|| { echo "Facebook Share callbacks must be idempotent." >&2; exit 1; }
contains_fixed_text "if !shown && !delegate.didFinish" "$share_interop_source" \
	|| { echo "Facebook Share must not invoke a second callback after ShareKit reports a synchronous failure." >&2; exit 1; }
contains_fixed_text "dialog.mode = .native" "$share_interop_source" \
	|| { echo "Facebook photo share must use the native Facebook dialog instead of the deprecated iOS share sheet." >&2; exit 1; }
contains_fixed_text "FacebookBridgePasteboard.clearPendingShareDataWithoutReading()" "$share_interop_source" \
	|| { echo "Facebook native share callbacks must clear bridge data without triggering an iOS paste read." >&2; exit 1; }
if contains_fixed_text "data(forPasteboardType:" "$share_interop_source"; then
	echo "Facebook Share wrapper must never read the general pasteboard." >&2
	exit 1
fi

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=False

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Share -p:ExpectedLoginEnabled=False -p:ExpectedShareEnabled=True

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Login%3BShare -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=True
