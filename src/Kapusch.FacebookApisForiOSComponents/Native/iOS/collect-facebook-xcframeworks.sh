#!/usr/bin/env bash
set -euo pipefail

# Copies the restored Facebook iOS SDK xcframeworks into build/fb/ in the
# layout packed into the NuGet and consumed by buildTransitive NativeReference
# items. Restores the pinned archives first when they are not present yet.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$ROOT_DIR/build"
UPSTREAM_DIR="$BUILD_DIR/upstream"
OUT_DIR="$BUILD_DIR/fb"

frameworks=(
	"FBAEMKit.xcframework"
	"FBSDKCoreKit.xcframework"
	"FBSDKCoreKit_Basics.xcframework"
	"FBSDKLoginKit.xcframework"
	"FBSDKShareKit.xcframework"
)

missing_upstream=false
for name in "${frameworks[@]}"; do
	if [ ! -d "$UPSTREAM_DIR/$name" ]; then
		missing_upstream=true
	fi
done

if [ "$missing_upstream" = true ]; then
	bash "$ROOT_DIR/restore-facebook-xcframeworks.sh"
fi

rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

for name in "${frameworks[@]}"; do
	src="$UPSTREAM_DIR/$name"
	if [ ! -d "$src" ]; then
		echo "Missing restored xcframework: $name" >&2
		exit 1
	fi

	dst="$OUT_DIR/$name"
	cp -R "$src" "$dst"

	# Keep iOS + iOS simulator only.
	find "$dst" -mindepth 1 -maxdepth 1 -type d -name "*-maccatalyst" -prune -exec rm -rf {} +

	# Strip debug symbols, signatures and Swift compile-time module artifacts.
	find "$dst" -type d -name "dSYMs" -prune -exec rm -rf {} +
	find "$dst" -type d -name "_CodeSignature" -prune -exec rm -rf {} +
	find "$dst" -type d -name "*.swiftmodule" -prune -exec rm -rf {} +
	find "$dst" -type f -name "*.bcsymbolmap" -delete || true

done

echo "Collected Facebook xcframeworks to: $OUT_DIR"
