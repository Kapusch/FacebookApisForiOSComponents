// C interop surface of the Facebook Share static library (kfbshare.xcframework).

#pragma once

#include "KapuschFacebookInteropExport.h"

typedef void (*KapuschFacebookShareCallback)(
	int32_t status,
	const char *errorCode,
	void *context);

KFB_EXPORT void kfb_facebook_share_configure_and_initialize(void *applicationPtr, bool trackingAllowed);

KFB_EXPORT void kfb_facebook_share_photo(
	void *presentingViewControllerPtr,
	const char *imagePathPtr,
	KapuschFacebookShareCallback callback,
	void *context);

KFB_EXPORT bool kfb_facebook_share_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr);
