// Intentionally minimal.
//
// This header exists only to satisfy `xcodebuild -create-xcframework -headers ...` when packaging the
// static library product. The interop surface is exposed via `@_cdecl` symbols in Swift and invoked
// from .NET via P/Invoke.
