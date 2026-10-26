// Shared export macro for the Kapusch Facebook interop static libraries.
//
// Every symbol marked with KFB_EXPORT is a plain C function consumed by .NET
// through `[LibraryImport("__Internal")]` (or a main-program symbol lookup for
// the historical Login entry points). `used` keeps the symbol in the archive
// even when nothing in the static library references it.

#pragma once

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
#define KFB_EXTERN_C extern "C"
#else
#define KFB_EXTERN_C
#endif

#define KFB_EXPORT KFB_EXTERN_C __attribute__((visibility("default"), used))
