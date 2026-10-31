// Facebook Login interop exported to .NET through C symbols.
//
// The facade is written against the Objective-C surface of the prebuilt
// Facebook iOS SDK xcframeworks. That surface is compiled by any recent clang,
// whereas the SDK's Swift interfaces only compile with the exact Swift compiler
// release Meta used to build the binaries.
//
// Threading: LoginKit presents UI and must run on the main thread. Every entry
// point hops to it when needed, and all the sign-in state below is only read
// and written there.

@import Foundation;
@import os.log;
@import UIKit;
@import FBSDKCoreKit;
@import FBSDKLoginKit;

#include "KapuschFacebookAuthInterop.h"
#include "KapuschFacebookSettingsRegistry.h"

typedef NS_ENUM(int32_t, KFBProviderStatus) {
	KFBProviderStatusSuccess = 0,
	KFBProviderStatusCancelled = 1,
	KFBProviderStatusFailed = 2,
};

typedef NS_ENUM(int32_t, KFBTrackingMode) {
	KFBTrackingModeEnabled = 0,
	KFBTrackingModeLimited = 1,
};

static os_log_t KFBLoginLog(void)
{
	static os_log_t log;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		NSString *subsystem = NSBundle.mainBundle.bundleIdentifier ?: @"Kapusch.Facebook.iOS";
		log = os_log_create(subsystem.UTF8String, "FacebookLogin");
	});
	return log;
}

static void KFBRunOnMainThread(dispatch_block_t block)
{
	if (NSThread.isMainThread) {
		block();
	} else {
		dispatch_async(dispatch_get_main_queue(), block);
	}
}

static NSString *KFBNonEmptyString(NSString *value)
{
	return value.length == 0 ? nil : value;
}

#pragma mark - Callback helpers

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

static void KFBInvokeSignInCancelled(KapuschFacebookSignInCallback callback, void *context)
{
	KFBInvokeSignInCallback(
		callback,
		KFBProviderStatusCancelled,
		nil,
		nil,
		nil,
		nil,
		nil,
		nil,
		context);
}

#pragma mark - Sign-in request

// One login started by kfb_facebook_signin_start. The managed callback is
// invoked exactly once per request: by the LoginKit completion or by
// kfb_facebook_signin_cancel, whichever comes first (`finished`).
@interface KFBSignInRequest : NSObject
@property (nonatomic, readonly) KapuschFacebookSignInCallback callback;
@property (nonatomic, readonly) void *context;
// Nonce of the LoginConfiguration: the value the SDK checked against the
// `nonce` claim of the authentication token it returns.
@property (nonatomic, readonly, copy) NSString *expectedNonce;
@property (nonatomic) BOOL finished;
// Kept alive while the request is pending; released once it finished.
@property (nonatomic, strong, nullable) FBSDKLoginManager *manager;
- (instancetype)initWithCallback:(KapuschFacebookSignInCallback)callback
						 context:(void *)context
				   expectedNonce:(NSString *)expectedNonce;
@end

@implementation KFBSignInRequest

- (instancetype)initWithCallback:(KapuschFacebookSignInCallback)callback
						 context:(void *)context
				   expectedNonce:(NSString *)expectedNonce
{
	self = [super init];
	if (self) {
		_callback = callback;
		_context = context;
		_expectedNonce = [expectedNonce copy];
	}
	return self;
}

@end

// The pending request, if any. Main thread only.
static KFBSignInRequest *KFBSignInRequestInFlight = nil;

// Marks the request finished and releases the in-flight state. Returns NO when
// it was already finished, in which case its callback must not run again.
static BOOL KFBFinishSignInRequest(KFBSignInRequest *request)
{
	if (request.finished) {
		return NO;
	}
	request.finished = YES;
	request.manager = nil;
	if (KFBSignInRequestInFlight == request) {
		KFBSignInRequestInFlight = nil;
	}
	return YES;
}

// A LoginKit completion arrived for a request the caller already cancelled. It
// is never reported. When it was a successful login, LoginKit has also stored
// its tokens as the SDK's current ones: clear them, unless another login is in
// flight or the current tokens are no longer the ones of this result, so the
// abandoned login does not leave the user signed in to the SDK.
static void KFBDiscardAbandonedSignIn(FBSDKLoginManagerLoginResult *result, NSError *error)
{
	os_log_info(KFBLoginLog(), "Discarded a Facebook login completion for a cancelled request.");
	if (result == nil || error != nil || result.isCancelled || KFBSignInRequestInFlight != nil) {
		return;
	}

	NSString *accessToken = KFBNonEmptyString(result.token.tokenString);
	NSString *authenticationToken = KFBNonEmptyString(result.authenticationToken.tokenString);
	BOOL storedAccessToken = accessToken != nil
		&& [FBSDKAccessToken.currentAccessToken.tokenString isEqualToString:accessToken];
	BOOL storedAuthenticationToken = authenticationToken != nil
		&& [FBSDKAuthenticationToken.currentAuthenticationToken.tokenString isEqualToString:authenticationToken];
	if (storedAccessToken || storedAuthenticationToken) {
		os_log_info(KFBLoginLog(), "Cleared the SDK tokens stored by the cancelled Facebook login.");
		[[FBSDKLoginManager new] logOut];
	}
}

// Maps the LoginKit completion of `request`. Main thread.
static void KFBCompleteSignIn(KFBSignInRequest *request, FBSDKLoginManagerLoginResult *result, NSError *error)
{
	if (!KFBFinishSignInRequest(request)) {
		KFBDiscardAbandonedSignIn(result, error);
		return;
	}

	KapuschFacebookSignInCallback callback = request.callback;
	void *context = request.context;

	// Same mapping as the SDK's Swift LoginResult: a missing result or any
	// error is a failure, then cancellation, then success.
	if (result == nil || error != nil) {
		NSError *failure = error ?: [NSError errorWithDomain:FBSDKLoginErrorDomain
														code:FBSDKLoginErrorUnknown
													userInfo:nil];
		KFBInvokeSignInFailure(
			callback,
			[NSString stringWithFormat:@"%@:%ld", failure.domain, (long)failure.code],
			failure.localizedDescription,
			context);
		return;
	}

	if (result.isCancelled) {
		KFBInvokeSignInCancelled(callback, context);
		return;
	}

	// Only the tokens of this result. The SDK's current tokens are globals: in
	// a reauthentication (an access token was already stored), LoginKit 18
	// replaces the current access token but leaves the current authentication
	// token of an earlier login in place.
	FBSDKAccessToken *token = result.token;
	FBSDKAuthenticationToken *authentication = result.authenticationToken;
	NSString *accessToken = KFBNonEmptyString(token.tokenString);
	NSString *authenticationToken = KFBNonEmptyString(authentication.tokenString);

	if (accessToken == nil && authenticationToken == nil) {
		KFBInvokeSignInFailure(
			callback,
			@"missing_token",
			@"Facebook sign-in returned neither an access token nor an authentication token.",
			context);
		return;
	}

	// The SDK only builds an authentication token whose `nonce` claim equals
	// the configured nonce; check it again so a token that does not belong to
	// this request is never reported.
	NSString *nonce = nil;
	if (authenticationToken != nil) {
		nonce = KFBNonEmptyString(authentication.nonce);
		if (nonce == nil || ![nonce isEqualToString:request.expectedNonce]) {
			KFBInvokeSignInFailure(
				callback,
				@"nonce_mismatch",
				@"The Facebook authentication token does not belong to this sign-in request.",
				context);
			return;
		}
	}

	KFBInvokeSignInCallback(
		callback,
		KFBProviderStatusSuccess,
		accessToken,
		authenticationToken,
		KFBNonEmptyString(token.userID),
		nonce,
		nil,
		nil,
		context);
}

static void KFBStartSignIn(
	UIViewController *presenting,
	int32_t trackingMode,
	NSString *nonce,
	KapuschFacebookSignInCallback callback,
	void *context)
{
	if (KFBSignInRequestInFlight != nil) {
		KFBInvokeSignInFailure(
			callback,
			@"already_in_progress",
			@"Facebook sign-in is already in progress.",
			context);
		return;
	}

	FBSDKLoginTracking tracking = trackingMode == KFBTrackingModeEnabled
		? FBSDKLoginTrackingEnabled
		: FBSDKLoginTrackingLimited;

	if (tracking == FBSDKLoginTrackingLimited && nonce == nil) {
		KFBInvokeSignInFailure(
			callback,
			@"missing_nonce",
			@"Facebook Limited Login requires a nonce.",
			context);
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
		return;
	}

	KFBSignInRequest *request = [[KFBSignInRequest alloc] initWithCallback:callback
																	context:context
															  expectedNonce:loginConfig.nonce];
	FBSDKLoginManager *manager = [FBSDKLoginManager new];
	request.manager = manager;
	KFBSignInRequestInFlight = request;

	[manager logInFromViewController:presenting
					   configuration:loginConfig
						  completion:^(FBSDKLoginManagerLoginResult *result, NSError *error) {
		KFBRunOnMainThread(^{
			KFBCompleteSignIn(request, result, error);
		});
	}];
}

#pragma mark - Exports

KFB_EXPORT void kfb_facebook_initialize(void *applicationPtr, void *launchOptionsPtr)
{
	UIApplication *application = (__bridge UIApplication *)applicationPtr;
	NSDictionary *launchOptions = launchOptionsPtr == NULL
		? nil
		: (__bridge NSDictionary *)launchOptionsPtr;

	KFBRunOnMainThread(^{
		// The App Events initialization (FacebookAppEvents.FinishedLaunching):
		// it applies again every setting the app recorded through
		// FacebookAppEvents and marks the SDK initialized for it.
		BOOL available = NO;
		KFBAppEventsFinishLaunching(application, launchOptions, &available);
		if (!available) {
			os_log_error(
				KFBLoginLog(),
				"Facebook SDK not initialized: the App Events facade of Kapusch.Facebook.iOS is not linked.");
		}
	});
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
	// Copy the arguments before a possible hop to the main thread: the caller
	// frees the nonce buffer when this function returns, and the strong
	// reference keeps the presenting controller alive until the login starts.
	UIViewController *presenting = (__bridge UIViewController *)presentingViewControllerPtr;
	NSString *nonce = nil;
	if (noncePtr != NULL) {
		NSString *trimmed = [[NSString stringWithUTF8String:noncePtr]
			stringByTrimmingCharactersInSet:NSCharacterSet.whitespaceAndNewlineCharacterSet];
		nonce = KFBNonEmptyString(trimmed);
	}

	KFBRunOnMainThread(^{
		KFBStartSignIn(presenting, trackingMode, nonce, callback, context);
	});
}

KFB_EXPORT void kfb_facebook_signin_cancel(void *context)
{
	// Always queued, never run inline: a start called off the main thread is
	// queued too, so the cancellation runs after it.
	dispatch_async(dispatch_get_main_queue(), ^{
		KFBSignInRequest *request = KFBSignInRequestInFlight;
		if (request == nil || request.context != context || !KFBFinishSignInRequest(request)) {
			return;
		}
		// LoginKit 18.0.2 has no public API to dismiss its login UI. It stays
		// up until the user closes it or a new login replaces it; its
		// completion is then discarded (KFBDiscardAbandonedSignIn). While it
		// is up, LoginKit reports the next login as cancelled at once, as it
		// tears the abandoned web authentication session down.
		os_log_info(KFBLoginLog(), "Facebook login cancelled by the caller.");
		KFBInvokeSignInCancelled(request.callback, request.context);
	});
}

KFB_EXPORT void kfb_facebook_signout(void)
{
	KFBRunOnMainThread(^{
		[[FBSDKLoginManager new] logOut];
	});
}
