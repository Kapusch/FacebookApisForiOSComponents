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

KFB_EXPORT void kfb_facebook_initialize(void *applicationPtr, void *launchOptionsPtr);

KFB_EXPORT bool kfb_facebook_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr);

KFB_EXPORT void kfb_facebook_signin_start(
	void *presentingViewControllerPtr,
	int32_t trackingMode,
	const char *noncePtr,
	KapuschFacebookSignInCallback callback,
	void *context);

KFB_EXPORT void kfb_facebook_signout(void);
