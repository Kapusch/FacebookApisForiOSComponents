#!/usr/bin/env bash
set -euo pipefail

# Builds the Objective-C interop static libraries against the pinned Facebook
# iOS SDK xcframeworks and wraps each one in an xcframework:
#   build/kfb.xcframework       (Login)
#   build/kfbshare.xcframework  (Share)
# It also leaves the stripped SDK xcframeworks in build/fb/.
#
# The facades only use the SDK's Objective-C headers (including the generated
# *-Swift.h headers). They therefore compile with any recent Xcode, even when
# Meta built the binaries with a newer Swift compiler than the local one.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_DIR="$ROOT_DIR/KapuschFacebookAuthInterop"
SOURCES_DIR="$PACKAGE_DIR/Sources"
HEADERS_DIR="$PACKAGE_DIR/include"
BUILD_DIR="$ROOT_DIR/build"
FB_DIR="$BUILD_DIR/fb"
OBJ_DIR="$BUILD_DIR/obj"
MODULE_CACHE_DIR="$BUILD_DIR/clang-module-cache"
DEPLOYMENT_TARGET="15.0"

rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

bash "$ROOT_DIR/restore-facebook-xcframeworks.sh"
bash "$ROOT_DIR/collect-facebook-xcframeworks.sh"

SDK_IPHONEOS_PATH="$(xcrun --sdk iphoneos --show-sdk-path)"
SDK_SIMULATOR_PATH="$(xcrun --sdk iphonesimulator --show-sdk-path)"

# Prints the slice directory of an xcframework for "device" or "simulator".
slice_dir() {
	local xcframework="$1"
	local platform="$2"
	local candidate
	for candidate in "$xcframework"/ios-*; do
		[ -d "$candidate" ] || continue
		case "$(basename "$candidate")" in
			*-maccatalyst) continue ;;
			*-simulator)
				if [ "$platform" = "simulator" ]; then
					echo "$candidate"
					return 0
				fi
				;;
			*)
				if [ "$platform" = "device" ]; then
					echo "$candidate"
					return 0
				fi
				;;
		esac
	done
	echo "No $platform slice found in $xcframework" >&2
	return 1
}

framework_search_flags() {
	local platform="$1"
	local xcframework
	for xcframework in "$FB_DIR"/*.xcframework; do
		printf -- '-F\n%s\n' "$(slice_dir "$xcframework" "$platform")"
	done
}

# compile_library <library name> <source dir> <platform> <arch> <sdk path> <triple> <output .a>
compile_library() {
	local library="$1"
	local source_dir="$2"
	local platform="$3"
	local arch="$4"
	local sdk_path="$5"
	local triple="$6"
	local output="$7"
	local obj_dir="$OBJ_DIR/$library/$platform-$arch"
	local search_flags=()
	local source
	local objects=()

	while IFS= read -r flag; do
		search_flags+=("$flag")
	done < <(framework_search_flags "$platform")

	mkdir -p "$obj_dir"
	for source in "$source_dir"/*.m; do
		local object="$obj_dir/$(basename "${source%.m}").o"
		xcrun clang \
			-c "$source" \
			-o "$object" \
			-target "$triple" \
			-isysroot "$sdk_path" \
			-fobjc-arc \
			-fmodules \
			-fmodules-cache-path="$MODULE_CACHE_DIR" \
			-O2 \
			-Wall \
			-Werror \
			-I "$HEADERS_DIR" \
			"${search_flags[@]}"
		objects+=("$object")
	done

	mkdir -p "$(dirname "$output")"
	xcrun libtool -static -no_warning_for_no_symbols -o "$output" "${objects[@]}"
}

# build_xcframework <library name> <source dir> <output xcframework>
build_xcframework() {
	local library="$1"
	local source_dir="$2"
	local xcframework_out="$3"
	local lib_dir="$BUILD_DIR/lib/$library"
	local device_lib="$lib_dir/iphoneos/lib$library.a"
	local sim_arm64_lib="$lib_dir/iphonesimulator-arm64/lib$library.a"
	local sim_x64_lib="$lib_dir/iphonesimulator-x86_64/lib$library.a"
	local sim_universal_lib="$lib_dir/iphonesimulator/lib$library.a"

	echo "[$library] Building (iOS device arm64)..."
	compile_library "$library" "$source_dir" device arm64 "$SDK_IPHONEOS_PATH" \
		"arm64-apple-ios$DEPLOYMENT_TARGET" "$device_lib"

	echo "[$library] Building (iOS simulator arm64)..."
	compile_library "$library" "$source_dir" simulator arm64 "$SDK_SIMULATOR_PATH" \
		"arm64-apple-ios$DEPLOYMENT_TARGET-simulator" "$sim_arm64_lib"

	echo "[$library] Building (iOS simulator x86_64)..."
	compile_library "$library" "$source_dir" simulator x86_64 "$SDK_SIMULATOR_PATH" \
		"x86_64-apple-ios$DEPLOYMENT_TARGET-simulator" "$sim_x64_lib"

	echo "[$library] Creating universal simulator static library..."
	mkdir -p "$(dirname "$sim_universal_lib")"
	lipo -create "$sim_arm64_lib" "$sim_x64_lib" -output "$sim_universal_lib"

	echo "[$library] Creating xcframework..."
	xcodebuild -create-xcframework \
		-library "$device_lib" -headers "$HEADERS_DIR" \
		-library "$sim_universal_lib" -headers "$HEADERS_DIR" \
		-output "$xcframework_out" >/dev/null

	echo "[$library] Done: $xcframework_out"
}

build_xcframework KapuschFacebookAuthInterop "$SOURCES_DIR/KapuschFacebookAuthInterop" "$BUILD_DIR/kfb.xcframework"
build_xcframework KapuschFacebookShareInterop "$SOURCES_DIR/KapuschFacebookShareInterop" "$BUILD_DIR/kfbshare.xcframework"
