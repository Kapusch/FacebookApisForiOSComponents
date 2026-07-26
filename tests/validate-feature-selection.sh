#!/usr/bin/env bash

set -euo pipefail

project="tests/FeatureSelection.proj"
share_source="src/Kapusch.FacebookApisForiOSComponents/Facebook/NativeFacebookShare.iOS.cs"

if rg -F "NativeLibrary.GetExport" "$share_source" >/dev/null; then
	echo "Facebook Share must use static __Internal imports so iOS retains its native symbols." >&2
	exit 1
fi

for symbol in \
	kfb_facebook_share_configure_and_initialize \
	kfb_facebook_share_handle_open_url \
	kfb_facebook_share_photo; do
	rg -F "EntryPoint = \"$symbol\"" "$share_source" >/dev/null \
		|| { echo "Missing static Facebook Share import: $symbol" >&2; exit 1; }
done

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=False

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Share -p:ExpectedLoginEnabled=False -p:ExpectedShareEnabled=True

dotnet msbuild "$project" -t:ValidateFeatureSelection -p:RequestedFacebookFeatures=Login%3BShare -p:ExpectedLoginEnabled=True -p:ExpectedShareEnabled=True
