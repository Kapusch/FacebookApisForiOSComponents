// Facebook Login interop exported to .NET through C symbols.
//
// The facade is written against the Objective-C surface of the prebuilt
// Facebook iOS SDK xcframeworks. That surface is compiled by any recent clang,
// whereas the SDK's Swift interfaces only compile with the exact Swift compiler
// release Meta used to build the binaries.

@import Foundation;
@import UIKit;
@import FBSDKCoreKit;
@import FBSDKLoginKit;

#include "KapuschFacebookAuthInterop.h"

typedef NS_ENUM(int32_t, KFBProviderStatus) {
	KFBProviderStatusSuccess = 0,
	KFBProviderStatusCancelled = 1,
	KFBProviderStatusFailed = 2,
};

typedef NS_ENUM(int32_t, KFBTrackingMode) {
	KFBTrackingModeEnabled = 0,
	KFBTrackingModeLimited = 1,
};

static BOOL KFBSignInInProgress = NO;
static KapuschFacebookSignInCallback KFBSignInCallback = NULL;
static void *KFBSignInContext = NULL;

static void KFBResetSignInState(void)
{
	KFBSignInInProgress = NO;
	KFBSignInCallback = NULL;
	KFBSignInContext = NULL;
}

static void KFBInvokeSignInCallback(
	KapuschFacebookSignInCallback callback,
	KFBProviderStatus status,
	NSString *accessToken,
	NSString *authenticationToken,
	NSString *userId,
	NSString *nonce,
	NSString *errorCode,
	NSString *errorMessage,
	void *context)
{
	// The UTF-8 buffers are owned by the NSString instances, which stay alive
	// for the duration of this synchronous call.
	callback(
		status,
		accessToken.UTF8String,
		authenticationToken.UTF8String,
		userId.UTF8String,
		nonce.UTF8String,
		errorCode.UTF8String,
		errorMessage.UTF8String,
		context);
}

static void KFBInvokeSignInFailure(
	KapuschFacebookSignInCallback callback,
	NSString *errorCode,
	NSString *errorMessage,
	void *context)
{
	KFBInvokeSignInCallback(
		callback,
		KFBProviderStatusFailed,
		nil,
		nil,
		nil,
		nil,
		errorCode,
		errorMessage,
		context);
}

KFB_EXPORT void kfb_facebook_initialize(void *applicationPtr, void *launchOptionsPtr)
{
	UIApplication *application = (__bridge UIApplication *)applicationPtr;
	NSDictionary *launchOptions = launchOptionsPtr == NULL
		? nil
		: (__bridge NSDictionary *)launchOptionsPtr;

	[FBSDKApplicationDelegate.sharedInstance application:application
							didFinishLaunchingWithOptions:launchOptions];
}

KFB_EXPORT bool kfb_facebook_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr)
{
	UIApplication *application = (__bridge UIApplication *)applicationPtr;
	NSURL *url = (__bridge NSURL *)urlPtr;
	NSDictionary *options = optionsPtr == NULL ? nil : (__bridge NSDictionary *)optionsPtr;
	NSDictionary<UIApplicationOpenURLOptionsKey, id> *openUrlOptions =
		[options isKindOfClass:NSDictionary.class] ? options : @{};

	return [FBSDKApplicationDelegate.sharedInstance application:application
														openURL:url
														options:openUrlOptions];
}

KFB_EXPORT void kfb_facebook_signin_start(
	void *presentingViewControllerPtr,
	int32_t trackingMode,
	const char *noncePtr,
	KapuschFacebookSignInCallback callback,
	void *context)
{
	if (KFBSignInInProgress) {
		KFBInvokeSignInFailure(
			callback,
			@"already_in_progress",
			@"Facebook sign-in is already in progress.",
			context);
		return;
	}

	KFBSignInInProgress = YES;
	KFBSignInCallback = callback;
	KFBSignInContext = context;

	UIViewController *presenting = (__bridge UIViewController *)presentingViewControllerPtr;
	FBSDKLoginTracking tracking = trackingMode == KFBTrackingModeEnabled
		? FBSDKLoginTrackingEnabled
		: FBSDKLoginTrackingLimited;

	NSString *nonce = nil;
	if (noncePtr != NULL) {
		NSString *trimmed = [[NSString stringWithUTF8String:noncePtr]
			stringByTrimmingCharactersInSet:NSCharacterSet.whitespaceAndNewlineCharacterSet];
		nonce = trimmed.length == 0 ? nil : trimmed;
	}

	if (tracking == FBSDKLoginTrackingLimited && nonce == nil) {
		KFBInvokeSignInFailure(
			callback,
			@"missing_nonce",
			@"Facebook Limited Login requires a nonce.",
			context);
		KFBResetSignInState();
		return;
	}

	NSArray<NSString *> *permissions = @[ @"public_profile", @"email" ];
	FBSDKLoginConfiguration *loginConfig = nonce != nil
		? [[FBSDKLoginConfiguration alloc] initWithPermissions:permissions
													  tracking:tracking
														 nonce:nonce]
		: [[FBSDKLoginConfiguration alloc] initWithPermissions:permissions
													  tracking:tracking
											   messengerPageId:nil];

	if (loginConfig == nil) {
		KFBInvokeSignInFailure(
			callback,
			@"invalid_login_configuration",
			@"Cannot create a valid Facebook LoginConfiguration.",
			context);
		KFBResetSignInState();
		return;
	}

	[[FBSDKLoginManager new]
		logInFromViewController:presenting
				  configuration:loginConfig
					 completion:^(FBSDKLoginManagerLoginResult *result, NSError *error) {
		KapuschFacebookSignInCallback pendingCallback = KFBSignInCallback;
		void *pendingContext = KFBSignInContext;
		if (pendingCallback == NULL || pendingContext == NULL) {
			KFBSignInInProgress = NO;
			return;
		}

		KFBResetSignInState();

		// Same mapping as the SDK's Swift LoginResult: a missing result or any
		// error is a failure, then cancellation, then success.
		if (result == nil || error != nil) {
			NSError *failure = error ?: [NSError errorWithDomain:FBSDKLoginErrorDomain
															code:FBSDKLoginErrorUnknown
														userInfo:nil];
			KFBInvokeSignInFailure(
				pendingCallback,
				[NSString stringWithFormat:@"%@:%ld", failure.domain, (long)failure.code],
				failure.localizedDescription,
				pendingContext);
			return;
		}

		if (result.isCancelled) {
			KFBInvokeSignInCallback(
				pendingCallback,
				KFBProviderStatusCancelled,
				nil,
				nil,
				nil,
				nil,
				nil,
				nil,
				pendingContext);
			return;
		}

		FBSDKAccessToken *token = result.token;
		FBSDKAuthenticationToken *currentAuthenticationToken =
			FBSDKAuthenticationToken.currentAuthenticationToken;
		NSString *accessToken = token.tokenString;
		NSString *authenticationToken = currentAuthenticationToken.tokenString;
		NSString *resolvedNonce = currentAuthenticationToken.nonce ?: nonce;
		NSString *userId = token.userID;

		if (accessToken != nil && accessToken.length == 0
			&& authenticationToken != nil && authenticationToken.length == 0) {
			KFBInvokeSignInFailure(
				pendingCallback,
				@"missing_token",
				@"Facebook sign-in returned no token.",
				pendingContext);
			return;
		}

		KFBInvokeSignInCallback(
			pendingCallback,
			KFBProviderStatusSuccess,
			accessToken,
			authenticationToken,
			userId,
			resolvedNonce,
			nil,
			nil,
			pendingContext);
	}];
}

KFB_EXPORT void kfb_facebook_signout(void)
{
	[[FBSDKLoginManager new] logOut];
}
