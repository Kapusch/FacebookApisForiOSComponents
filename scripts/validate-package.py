#!/usr/bin/env python3
"""Validates the packed Kapusch.Facebook.iOS NuGet and the App Events bindings.

Usage: python3 scripts/validate-package.py [path/to/package.nupkg]
(defaults to the newest artifacts/nuget/*.nupkg)

Checks:
1. Package layout: every wrapper and Facebook SDK xcframework is present,
   nothing is flattened at the root and no unwanted SDK kit is shipped. Each
   SDK xcframework has the device and simulator slices that
   buildTransitive/Kapusch.Facebook.iOS.targets references, each with the
   privacy manifest the targets bundle.
2. Every C entry point declared by the wrapper headers is exported by both
   slices of its static library.
3. Every kfb_appevents_* entry point is bound by FacebookAppEvents with a
   static __Internal import, and no Login / Share / App Events binding
   resolves symbols at run time.
4. The managed constants cover every App Events name the bundled SDK headers
   declare, and every constant value exists in the bundled FBSDKCoreKit binary.
"""
from __future__ import annotations

import glob
import re
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "Kapusch.FacebookApisForiOSComponents"
HEADERS = SRC / "Native" / "iOS" / "KapuschFacebookAuthInterop" / "include"
FACEBOOK_CS = SRC / "Facebook"

WRAPPERS = {
    "kfb.xcframework": ("libKapuschFacebookAuthInterop.a", "KapuschFacebookAuthInterop.h"),
    "kfbloginstub.xcframework": ("libKapuschFacebookLoginStub.a", "KapuschFacebookAuthInterop.h"),
    "kfbshare.xcframework": ("libKapuschFacebookShareInterop.a", "KapuschFacebookShareInterop.h"),
    "kfbsharestub.xcframework": ("libKapuschFacebookShareStub.a", "KapuschFacebookShareInterop.h"),
    "kfbappevents.xcframework": ("libKapuschFacebookAppEventsInterop.a", "KapuschFacebookAppEventsInterop.h"),
}

FACEBOOK_FRAMEWORKS = [
    "FBAEMKit",
    "FBSDKCoreKit",
    "FBSDKCoreKit_Basics",
    "FBSDKLoginKit",
    "FBSDKShareKit",
]

# Slice directories hard-coded by buildTransitive/Kapusch.Facebook.iOS.targets.
TARGET_SLICES = ["ios-arm64", "ios-arm64_x86_64-simulator"]

FORBIDDEN_PREFIXES = [
    "fb/FBSDKGamingServicesKit.xcframework/",
]

# Header -> Objective-C symbol prefix of the constants the managed API must cover.
CONSTANT_HEADERS = {
    "FBSDKAppEventName.h": "FBSDKAppEventName",
    "FBSDKAppEventParameterName.h": "FBSDKAppEventParameterName",
    "FBSDKAppEventParameterValue.h": "FBSDKAppEventParameterValue",
    "FBSDKAppEventUserDataType.h": "FBSDKAppEvent",
    "FBSDKAppEventParameterProduct.h": "FBSDKAppEventParameterProduct",
}

CONSTANT_SOURCES = [
    FACEBOOK_CS / "FacebookAppEventNames.cs",
    FACEBOOK_CS / "FacebookAppEventParameterNames.cs",
]

errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def exported_symbols(header: Path) -> list[str]:
    text = header.read_text()
    return re.findall(r"^KFB_EXPORT\s+[^;{]*?\b(kfb_[a-z0-9_]+)\s*\(", text, re.MULTILINE)


def nm_symbols(library: Path) -> set[str]:
    output = subprocess.check_output(["nm", "-gUj", str(library)], text=True, stderr=subprocess.DEVNULL)
    return {line.strip().lstrip("_") for line in output.splitlines() if line.strip()}


def check_layout(names: set[str]) -> None:
    required = ["buildTransitive/Kapusch.Facebook.iOS.props", "buildTransitive/Kapusch.Facebook.iOS.targets"]
    required += [f"{wrapper}/Info.plist" for wrapper in WRAPPERS]
    required += [f"fb/{framework}.xcframework/Info.plist" for framework in FACEBOOK_FRAMEWORKS]
    for path in required:
        if path not in names:
            fail(f"missing package entry: {path}")

    if "Info.plist" in names:
        fail("a wrapper appears flattened (found 'Info.plist' at the package root)")

    for framework in FACEBOOK_FRAMEWORKS:
        for slice_name in TARGET_SLICES:
            manifest = f"fb/{framework}.xcframework/{slice_name}/{framework}.framework/PrivacyInfo.xcprivacy"
            if manifest not in names:
                fail(f"missing {manifest} (referenced by Kapusch.Facebook.iOS.targets)")

    targets = (SRC / "buildTransitive" / "Kapusch.Facebook.iOS.targets").read_text()
    for slice_name in TARGET_SLICES:
        if slice_name not in targets:
            fail(f"Kapusch.Facebook.iOS.targets no longer references the {slice_name} slice; update TARGET_SLICES")

    for prefix in FORBIDDEN_PREFIXES:
        if any(name.startswith(prefix) for name in names):
            fail(f"forbidden package content: {prefix}")


def check_exports(extracted: Path) -> None:
    for wrapper, (library, header) in WRAPPERS.items():
        expected = exported_symbols(HEADERS / header)
        if not expected:
            fail(f"no KFB_EXPORT symbols parsed from {header}")
            continue
        slices = sorted((extracted / wrapper).glob(f"ios-*/{library}"))
        if len(slices) != 2:
            fail(f"{wrapper}: expected a device and a simulator slice of {library}, found {len(slices)}")
        for slice_library in slices:
            missing = sorted(set(expected) - nm_symbols(slice_library))
            if missing:
                fail(f"{slice_library.relative_to(extracted)} does not export: {', '.join(missing)}")


def check_app_events_bindings() -> None:
    source = (FACEBOOK_CS / "FacebookAppEvents.iOS.cs").read_text()
    for symbol in exported_symbols(HEADERS / "KapuschFacebookAppEventsInterop.h"):
        if f'EntryPoint = "{symbol}"' not in source:
            fail(f"FacebookAppEvents does not bind {symbol}")
    if 'private const string NativeLibraryName = "__Internal";' not in source:
        fail("FacebookAppEvents must bind its entry points with static __Internal imports")
    for name in ["FacebookAppEvents.iOS.cs", "NativeFacebookLogin.iOS.cs", "NativeFacebookShare.iOS.cs"]:
        if "NativeLibrary.GetExport" in (FACEBOOK_CS / name).read_text():
            fail(f"{name} must use static __Internal imports so iOS keeps its native symbols")


def check_constants(extracted: Path) -> None:
    core = sorted((extracted / "fb" / "FBSDKCoreKit.xcframework").glob("ios-*/FBSDKCoreKit.framework"))
    device = [path for path in core if "simulator" not in path.parent.name]
    if not device:
        fail("FBSDKCoreKit device slice not found")
        return
    framework = device[0]

    managed = "\n".join(path.read_text() for path in CONSTANT_SOURCES)
    for header, prefix in CONSTANT_HEADERS.items():
        text = (framework / "Headers" / header).read_text()
        symbols = re.findall(rf"FOUNDATION_EXPORT\s+\w+\s+(?:const\s+)?({prefix}\w+)\s*", text)
        if not symbols:
            fail(f"no exported constants parsed from {header}")
        for symbol in symbols:
            if not re.search(rf"\b{symbol}\b", managed):
                fail(f"managed constants do not cover {symbol} ({header})")

    binary = (framework / "FBSDKCoreKit").read_bytes()
    for value in re.findall(r'public const string \w+ = "([^"]+)";', managed):
        if len(value) > 1 and value.encode() not in binary:
            fail(f'constant value "{value}" not found in the bundled FBSDKCoreKit binary')


def main() -> int:
    if len(sys.argv) > 1:
        nupkg = Path(sys.argv[1])
    else:
        candidates = sorted(glob.glob(str(ROOT / "artifacts" / "nuget" / "*.nupkg")), key=lambda p: Path(p).stat().st_mtime)
        if not candidates:
            print("ERROR: no .nupkg found under artifacts/nuget/", file=sys.stderr)
            return 1
        nupkg = Path(candidates[-1])

    print(f"Validating {nupkg}")
    with zipfile.ZipFile(nupkg) as archive, tempfile.TemporaryDirectory() as temp:
        names = set(archive.namelist())
        check_layout(names)
        archive.extractall(temp)
        extracted = Path(temp)
        check_exports(extracted)
        check_app_events_bindings()
        check_constants(extracted)

    if errors:
        print("ERROR: package validation failed:", file=sys.stderr)
        for error in errors:
            print(f"  - {error}", file=sys.stderr)
        return 1

    print("OK: package layout, native exports, App Events bindings and constants are valid.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
