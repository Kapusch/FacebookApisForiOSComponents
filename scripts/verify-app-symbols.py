#!/usr/bin/env python3
"""Checks that a built app executable contains every kfb_* entry point.

Usage: python3 scripts/verify-app-symbols.py path/to/App.app/AppExecutable

The managed bindings use static __Internal imports, so the .NET iOS build must
keep every entry point of the wrapper headers in the app, whatever
KapuschFacebookFeatures selects (the Login and Share placeholders define the
same symbols). A missing symbol means the native linker dead-stripped it and the
managed call would fail at run time.
"""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HEADERS = ROOT / "src" / "Kapusch.FacebookApisForiOSComponents" / "Native" / "iOS" / "KapuschFacebookAuthInterop" / "include"
WRAPPER_HEADERS = [
    "KapuschFacebookAuthInterop.h",
    "KapuschFacebookShareInterop.h",
    "KapuschFacebookAppEventsInterop.h",
]


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2
    executable = Path(sys.argv[1])
    expected = set()
    for header in WRAPPER_HEADERS:
        text = (HEADERS / header).read_text()
        expected.update(re.findall(r"^KFB_EXPORT\s+[^;{]*?\b(kfb_[a-z0-9_]+)\s*\(", text, re.MULTILINE))
    output = subprocess.check_output(["nm", "-gUj", str(executable)], text=True)
    present = {line.strip().lstrip("_") for line in output.splitlines()}
    missing = sorted(expected - present)
    if missing:
        print(f"ERROR: {executable.name} lacks {len(missing)} entry point(s): {', '.join(missing)}", file=sys.stderr)
        return 1
    print(f"OK: {executable.name} contains all {len(expected)} kfb_* entry points.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
