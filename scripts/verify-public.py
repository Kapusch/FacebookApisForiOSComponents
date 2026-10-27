#!/usr/bin/env python3
"""Fail when a tracked file contains a real-looking credential, an email or a private term.

This repository is a public, generic wrapper. The generic checks below are tracked. Terms that
must never appear (for example the names of private consuming apps) are not: they are read at run
time from an untracked source so the scanner never publishes them.

Private terms, one per line or comma-separated, matched case-insensitively as literal text:
  - the KAPUSCH_PRIVATE_TERMS environment variable (CI reads it from a repository secret);
  - the file <git dir>/info/private-terms ('#' starts a comment).
The private-term check is skipped, with a notice, when neither source provides a term.
Failures name the file and the term's position, never the term itself.
"""

from __future__ import annotations

import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

PATTERNS = {
    # Meta app IDs are 15-16 digit numbers; docs and samples must not carry a real one.
    "Meta app ID or fb<appId> URL scheme": re.compile(
        rb"(?i)(?:FacebookAppID</key>\s*<string>|\bfb)[1-9][0-9]{14,15}\b"
    ),
    # Client tokens are 32 hex characters; an all-zero placeholder is allowed.
    "Meta client token": re.compile(rb"(?i)FacebookClientToken</key>\s*<string>(?!0{32}<)[0-9a-f]{32}<"),
    "email address": re.compile(rb"[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+\.[A-Za-z.]{2,}"),
}
# Files whose own source would match the generic patterns. Private terms are checked everywhere.
GENERIC_SKIP = {"scripts/verify-public.py"}


def split_terms(text: str) -> list[str]:
    terms = []
    for line in text.splitlines():
        line = line.split("#", 1)[0]
        terms.extend(part.strip() for part in line.split(","))
    return [term for term in terms if term]


def load_private_terms() -> list[str]:
    terms = split_terms(os.environ.get("KAPUSCH_PRIVATE_TERMS", ""))
    git_dir = subprocess.check_output(["git", "rev-parse", "--absolute-git-dir"], cwd=ROOT, text=True).strip()
    terms_file = Path(git_dir) / "info" / "private-terms"
    if terms_file.is_file():
        terms.extend(split_terms(terms_file.read_text(encoding="utf-8")))
    return list(dict.fromkeys(terms))


def main() -> int:
    files = [f for f in subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT).decode().split("\0") if f]
    private = [
        (f"private term #{index}", re.compile(re.escape(term.encode()), re.IGNORECASE))
        for index, term in enumerate(load_private_terms(), start=1)
    ]
    errors = []
    for name in files:
        path = ROOT / name
        if not path.is_file():
            continue
        data = path.read_bytes()
        checks = list(private)
        if name not in GENERIC_SKIP:
            checks.extend(PATTERNS.items())
        for label, pattern in checks:
            if pattern.search(data):
                errors.append(f"{name}: {label}")
    if errors:
        print("Public tree scan failed:\n  " + "\n  ".join(errors), file=sys.stderr)
        return 1
    if private:
        scope = f"{len(private)} private term(s)"
    else:
        scope = "no private terms configured (set KAPUSCH_PRIVATE_TERMS or .git/info/private-terms)"
    print(f"Public tree scan passed ({len(files)} tracked files, {scope})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
