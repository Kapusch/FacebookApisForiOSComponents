// C interop surface of the Facebook Login static library (kfb.xcframework).

#pragma once

#include "KapuschFacebookInteropExport.h"

typedef void (*KapuschFacebookSignInCallback)(
	int32_t status,
	const char *accessToken,
	const char *authenticationToken,
	const char *userId,
	const char *nonce,
	const char *errorCode,
	const char *errorMessage,
	void *context);

// Initializes the SDK through the App Events initialization
// (FacebookAppEvents.FinishedLaunching), on the main thread.
KFB_EXPORT void kfb_facebook_initialize(void *applicationPtr, void *launchOptionsPtr);

KFB_EXPORT bool kfb_facebook_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr);

// Starts a login on the main thread (hopping to it when needed). `noncePtr` is
// copied before this function returns. `callback` is invoked exactly once per
// call, on the main thread, possibly before this function returns. `context`
// identifies the request for kfb_facebook_signin_cancel and must not be reused
// by the caller while the request may still be pending.
KFB_EXPORT void kfb_facebook_signin_start(
	void *presentingViewControllerPtr,
	int32_t trackingMode,
	const char *noncePtr,
	KapuschFacebookSignInCallback callback,
	void *context);

// Abandons the pending login started with `context`, if any: its callback is
// invoked with the Cancelled status and the in-flight state is released, so a
// new login can start. A later LoginKit completion for that request is
// discarded. Always queued on the main thread. No-op for an unknown context.
KFB_EXPORT void kfb_facebook_signin_cancel(void *context);

KFB_EXPORT void kfb_facebook_signout(void);
