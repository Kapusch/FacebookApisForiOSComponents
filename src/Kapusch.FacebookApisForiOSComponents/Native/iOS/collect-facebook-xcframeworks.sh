#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$ROOT_DIR/build"
SCRATCH_DIR="$BUILD_DIR/spm"
OUT_DIR="$BUILD_DIR/fb"

if [ ! -d "$SCRATCH_DIR" ]; then
	echo "SwiftPM scratch dir not found: $SCRATCH_DIR" >&2
	echo "Run build.sh first." >&2
	exit 1
fi

rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

frameworks=(
	"FBAEMKit.xcframework"
	"FBSDKCoreKit.xcframework"
	"FBSDKCoreKit_Basics.xcframework"
	"FBSDKGamingServicesKit.xcframework"
	"FBSDKLoginKit.xcframework"
	"FBSDKShareKit.xcframework"
)

for name in "${frameworks[@]}"; do
	src="$(find "$SCRATCH_DIR" -maxdepth 8 -type d -name "$name" | head -n 1 || true)"
	if [ -z "$src" ] || [ ! -d "$src" ]; then
		echo "Missing xcframework in scratch: $name" >&2
		exit 1
	fi

	dst="$OUT_DIR/$name"
	cp -R "$src" "$dst"

	# Keep iOS + iOS simulator only.
	rm -rf "$dst/ios-arm64_x86_64-maccatalyst" 2>/dev/null || true

	# Strip debug symbols, signatures and Swift compile-time module artifacts.
	find "$dst" -type d -name "dSYMs" -prune -exec rm -rf {} +
	find "$dst" -type d -name "_CodeSignature" -prune -exec rm -rf {} +
	find "$dst" -type d -name "*.swiftmodule" -prune -exec rm -rf {} +
	find "$dst" -type f -name "*.bcsymbolmap" -delete || true

done

echo "Collected Facebook xcframeworks to: $OUT_DIR"
