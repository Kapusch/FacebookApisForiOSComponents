# Samples

This repo can be validated either by consuming the NuGet from your app, or via the small local iOS sample.

Principles:
- No secrets committed.
- Keep `Info.plist` values in local-only files (ignored by `.gitignore`).

## iOS

- `samples/Kapusch.Facebook.iOS.Sample/` — minimal UIKit app that links against the wrapper + Facebook xcframeworks (local validation).
- Manual CI: `.github/workflows/sample-ios.yml` (run via `workflow_dispatch`).
