// Placeholder for the Login entry points, linked when `Login` is not selected.
//
// NativeFacebookLogin binds its entry points with `[LibraryImport("__Internal")]`,
// so the .NET iOS build asks the native linker for these symbols whatever the
// selected features are. This placeholder depends on Foundation only and never
// touches the Facebook SDK.

@import Foundation;
@import os.log;

#include "KapuschFacebookAuthInterop.h"

static const int32_t KFBLoginStubStatusFailed = 2;

static void KFBLogLoginNotSelected(void)
{
	os_log_error(
		OS_LOG_DEFAULT,
		"Kapusch.Facebook.iOS: Facebook Login is not selected. Add Login to KapuschFacebookFeatures.");
}

KFB_EXPORT void kfb_facebook_initialize(void *applicationPtr, void *launchOptionsPtr)
{
	KFBLogLoginNotSelected();
}

KFB_EXPORT bool kfb_facebook_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr)
{
	return false;
}

KFB_EXPORT void kfb_facebook_signin_start(
	void *presentingViewControllerPtr,
	int32_t trackingMode,
	const char *noncePtr,
	KapuschFacebookSignInCallback callback,
	void *context)
{
	KFBLogLoginNotSelected();
	callback(
		KFBLoginStubStatusFailed,
		NULL,
		NULL,
		NULL,
		NULL,
		"login_feature_not_selected",
		"Facebook Login is not selected in KapuschFacebookFeatures.",
		context);
}

KFB_EXPORT void kfb_facebook_signout(void)
{
	KFBLogLoginNotSelected();
}
