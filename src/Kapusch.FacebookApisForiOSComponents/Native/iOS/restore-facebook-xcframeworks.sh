#!/usr/bin/env bash
set -euo pipefail

# Restores the Facebook iOS SDK dynamic xcframeworks pinned in
# DependencyLocks/iOS/lockstate.txt. Every archive is verified against its
# SHA-256 before it is extracted; a mismatch fails the build.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$ROOT_DIR/../../../.." && pwd)"
LOCK_FILE="$REPO_ROOT/DependencyLocks/iOS/lockstate.txt"
CACHE_DIR="${KAPUSCH_FACEBOOK_IOS_DOWNLOAD_CACHE:-$ROOT_DIR/.cache/downloads}"
UPSTREAM_DIR="$ROOT_DIR/build/upstream"

if [ ! -f "$LOCK_FILE" ]; then
	echo "Missing lockstate.txt (expected at $LOCK_FILE)." >&2
	exit 1
fi

sha256_file() {
	local file="$1"
	if command -v shasum >/dev/null 2>&1; then
		shasum -a 256 "$file" | awk '{print $1}'
		return 0
	fi
	if command -v sha256sum >/dev/null 2>&1; then
		sha256sum "$file" | awk '{print $1}'
		return 0
	fi
	echo "No SHA-256 tool found (need shasum or sha256sum)." >&2
	return 1
}

mkdir -p "$CACHE_DIR"
rm -rf "$UPSTREAM_DIR"
mkdir -p "$UPSTREAM_DIR"

echo "[KapuschFacebookInterop] Restoring pinned Facebook iOS SDK artifacts..."

restored_any=false

while IFS= read -r line || [ -n "$line" ]; do
	case "$line" in
		''|'#'*) continue ;;
	esac

	url="$(echo "$line" | awk '{print $1}')"
	expected_sha="$(echo "$line" | awk '{print $2}')"
	filename="$(echo "$line" | awk '{print $3}')"

	if [ -z "$url" ] || [ -z "$expected_sha" ] || [ -z "$filename" ]; then
		echo "Invalid lock line (expected: <url> <sha256> <filename>): $line" >&2
		exit 1
	fi

	archive="$CACHE_DIR/$filename"
	if [ -f "$archive" ] && [ "$(sha256_file "$archive")" = "$expected_sha" ]; then
		echo "[KapuschFacebookInterop] Using cached $filename"
	else
		echo "[KapuschFacebookInterop] Downloading $filename"
		curl -fsSL --retry 3 --retry-delay 1 -o "$archive.partial" "$url"
		actual_sha="$(sha256_file "$archive.partial")"
		if [ "$actual_sha" != "$expected_sha" ]; then
			rm -f "$archive.partial"
			echo "SHA256 mismatch for $filename" >&2
			echo "Expected: $expected_sha" >&2
			echo "Actual:   $actual_sha" >&2
			exit 1
		fi
		mv -f "$archive.partial" "$archive"
	fi

	unzip -q -o "$archive" -d "$UPSTREAM_DIR"
	restored_any=true
done < "$LOCK_FILE"

if [ "$restored_any" != true ]; then
	echo "No artifacts restored (lock file empty?)." >&2
	exit 1
fi

echo "[KapuschFacebookInterop] Restored upstream xcframeworks to: $UPSTREAM_DIR"
