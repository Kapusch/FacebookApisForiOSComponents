#!/usr/bin/env python3
"""Fails when a tracked file contains a private identifier.

This is a public, generic package: docs, samples and tests must not carry a
consuming app's name or real Facebook credentials. The app-name pattern is
hex-escaped so this script does not contain the name itself.
"""
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]

PATTERNS = {
    "consuming app name": re.compile(rb"(?i)\x62\x6c\x6f\x6f\x6d[ &_-]*(?:and[ _-]*)?(?:fly|y)"),
    "real Facebook URL scheme": re.compile(rb"\bfb[0-9]{8,}\b"),
    "real Facebook app ID": re.compile(rb"FacebookAppID</key>\s*<string>[0-9]{8,}"),
    "real Facebook client token": re.compile(rb"FacebookClientToken</key>\s*<string>[0-9a-f]{32}"),
}


def main() -> int:
    files = subprocess.check_output(["git", "ls-files"], cwd=ROOT, text=True).splitlines()
    failures = []
    for name in files:
        path = ROOT / name
        if not path.is_file():
            continue
        data = path.read_bytes()
        for label, pattern in PATTERNS.items():
            if pattern.search(data):
                failures.append(f"{name}: {label}")

    if failures:
        print("Private identifiers found in tracked files:", file=sys.stderr)
        for failure in failures:
            print(f"  - {failure}", file=sys.stderr)
        return 1

    print("Public tree scan passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
