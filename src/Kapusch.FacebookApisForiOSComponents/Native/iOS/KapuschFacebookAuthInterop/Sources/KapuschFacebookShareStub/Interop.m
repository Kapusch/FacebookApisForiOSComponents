// Placeholder for the Share entry points, linked when `Share` is not selected.
//
// NativeFacebookShare binds its entry points with `[LibraryImport("__Internal")]`,
// so the .NET iOS build asks the native linker for these symbols whatever the
// selected features are. Without this placeholder, a Login-only or
// AppEvents-only app fails to link. It depends on Foundation only and never
// touches the Facebook SDK.

@import Foundation;
@import os.log;

#include "KapuschFacebookShareInterop.h"

static const int32_t KFBShareStubStatusFailed = 2;

static void KFBLogShareNotSelected(void)
{
	os_log_error(
		OS_LOG_DEFAULT,
		"Kapusch.Facebook.iOS: Facebook Share is not selected. Add Share to KapuschFacebookFeatures.");
}

KFB_EXPORT void kfb_facebook_share_configure_and_initialize(void *applicationPtr, bool trackingAllowed)
{
	KFBLogShareNotSelected();
}

KFB_EXPORT void kfb_facebook_share_photo(
	void *presentingViewControllerPtr,
	const char *imagePathPtr,
	KapuschFacebookShareCallback callback,
	void *context)
{
	KFBLogShareNotSelected();
	callback(KFBShareStubStatusFailed, "share_feature_not_selected", context);
}

KFB_EXPORT bool kfb_facebook_share_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr)
{
	return false;
}
