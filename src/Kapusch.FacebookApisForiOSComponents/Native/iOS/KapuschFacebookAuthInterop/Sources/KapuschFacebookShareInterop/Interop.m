// Facebook photo-share interop exported to .NET through C symbols.
//
// Written against the Objective-C surface of the prebuilt Facebook iOS SDK so
// it does not depend on the Swift compiler release Meta used for the binaries.

@import Foundation;
@import os.log;
@import UIKit;
@import FBSDKCoreKit;
@import FBSDKShareKit;

#include "KapuschFacebookShareInterop.h"

typedef NS_ENUM(int32_t, KFBShareStatus) {
	KFBShareStatusSuccess = 0,
	KFBShareStatusCancelled = 1,
	KFBShareStatusFailed = 2,
};

static NSString *const KFBFacebookBridgePasteboardType = @"com.facebook.Facebook.FBAppBridgeType";

static os_log_t KFBShareLog(void)
{
	static os_log_t log;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		NSString *subsystem = NSBundle.mainBundle.bundleIdentifier ?: @"Kapusch.Facebook.iOS";
		log = os_log_create(subsystem.UTF8String, "FacebookShare");
	});
	return log;
}

// Meta's app bridge leaves a private marker on the general pasteboard. Replace
// it with empty data without reading the pasteboard, so iOS never shows a paste
// authorization prompt and unrelated pasteboard content is left untouched.
static void KFBClearPendingShareDataWithoutReading(void)
{
	[UIPasteboard.generalPasteboard setData:[NSData data]
						  forPasteboardType:KFBFacebookBridgePasteboardType];
	os_log_debug(KFBShareLog(), "Cleared pending Facebook bridge data without reading the pasteboard.");
}

static void KFBInvokeShareCallback(
	KapuschFacebookShareCallback callback,
	KFBShareStatus status,
	NSString *errorCode,
	void *context)
{
	callback(status, errorCode.UTF8String, context);
}

@class KFBShareDelegate;

static FBSDKShareDialog *KFBShareDialogInFlight = nil;
static KFBShareDelegate *KFBShareDelegateInFlight = nil;
static BOOL KFBShareSdkInitialized = NO;

static void KFBClearShareState(void)
{
	KFBShareDialogInFlight = nil;
	KFBShareDelegateInFlight = nil;
}

@interface KFBShareDelegate : NSObject <FBSDKSharingDelegate>
@property (nonatomic, readonly) BOOL didFinish;
- (instancetype)initWithCallback:(KapuschFacebookShareCallback)callback context:(void *)context;
@end

@implementation KFBShareDelegate {
	KapuschFacebookShareCallback _callback;
	void *_context;
}

- (instancetype)initWithCallback:(KapuschFacebookShareCallback)callback context:(void *)context
{
	self = [super init];
	if (self) {
		_callback = callback;
		_context = context;
		_didFinish = NO;
	}
	return self;
}

// Every completion path is guarded so the managed task completes at most once,
// including when ShareKit reports a synchronous failure and a late callback.
- (void)sharer:(id<FBSDKSharing>)sharer didCompleteWithResults:(NSDictionary<NSString *, id> *)results
{
	if (_didFinish) {
		return;
	}
	_didFinish = YES;
	os_log_info(KFBShareLog(), "Facebook share completed.");
	KFBInvokeShareCallback(_callback, KFBShareStatusSuccess, nil, _context);
	KFBClearShareState();
}

- (void)sharer:(id<FBSDKSharing>)sharer didFailWithError:(NSError *)error
{
	if (_didFinish) {
		return;
	}
	_didFinish = YES;
	os_log_error(
		KFBShareLog(),
		"Facebook share failed. domain=%{public}@ code=%ld",
		error.domain,
		(long)error.code);
	KFBInvokeShareCallback(
		_callback,
		KFBShareStatusFailed,
		[NSString stringWithFormat:@"%@:%ld", error.domain, (long)error.code],
		_context);
	KFBClearShareState();
}

- (void)sharerDidCancel:(id<FBSDKSharing>)sharer
{
	if (_didFinish) {
		return;
	}
	_didFinish = YES;
	os_log_info(KFBShareLog(), "Facebook share cancelled.");
	KFBInvokeShareCallback(_callback, KFBShareStatusCancelled, nil, _context);
	KFBClearShareState();
}

@end

KFB_EXPORT void kfb_facebook_share_configure_and_initialize(void *applicationPtr, bool trackingAllowed)
{
	os_log_debug(
		KFBShareLog(),
		"Configuring Facebook Share SDK. trackingAllowed=%{public}d",
		trackingAllowed ? 1 : 0);
	FBSDKSettings *settings = FBSDKSettings.sharedSettings;
	settings.isAutoLogAppEventsEnabled = NO;
	settings.isAdvertiserIDCollectionEnabled = trackingAllowed;
	settings.isAdvertiserTrackingEnabled = trackingAllowed;
	if (KFBShareSdkInitialized) {
		return;
	}

	UIApplication *application = (__bridge UIApplication *)applicationPtr;
	[FBSDKApplicationDelegate.sharedInstance application:application
							didFinishLaunchingWithOptions:nil];
	KFBShareSdkInitialized = YES;
}

KFB_EXPORT void kfb_facebook_share_photo(
	void *presentingViewControllerPtr,
	const char *imagePathPtr,
	KapuschFacebookShareCallback callback,
	void *context)
{
	os_log_debug(KFBShareLog(), "Facebook photo share requested.");
	if (KFBShareDialogInFlight != nil) {
		os_log_error(KFBShareLog(), "Facebook share rejected because another request is in progress.");
		KFBInvokeShareCallback(callback, KFBShareStatusFailed, @"already_in_progress", context);
		return;
	}

	NSString *imagePath = [NSString stringWithUTF8String:imagePathPtr];
	UIImage *image = imagePath == nil ? nil : [UIImage imageWithContentsOfFile:imagePath];
	if (image == nil) {
		os_log_error(KFBShareLog(), "Facebook share could not decode the image.");
		KFBInvokeShareCallback(callback, KFBShareStatusFailed, @"image_decode_failed", context);
		return;
	}

	UIViewController *presenting = (__bridge UIViewController *)presentingViewControllerPtr;
	FBSDKSharePhoto *photo = [[FBSDKSharePhoto alloc] initWithImage:image isUserGenerated:NO];
	FBSDKSharePhotoContent *content = [FBSDKSharePhotoContent new];
	content.photos = @[ photo ];
	KFBShareDelegate *delegate = [[KFBShareDelegate alloc] initWithCallback:callback context:context];
	FBSDKShareDialog *dialog = [[FBSDKShareDialog alloc] initWithViewController:presenting
																		content:content
																	   delegate:delegate];
	dialog.mode = FBSDKShareDialogModeNative;
	// ShareDialog holds its delegate weakly: keep both alive until completion.
	KFBShareDelegateInFlight = delegate;
	KFBShareDialogInFlight = dialog;
	os_log_debug(KFBShareLog(), "Facebook ShareDialog canShow=%{public}d", dialog.canShow ? 1 : 0);
	BOOL shown = [dialog show];
	os_log_debug(KFBShareLog(), "Facebook ShareDialog show returned %{public}d", shown ? 1 : 0);
	if (!shown && !delegate.didFinish) {
		os_log_error(KFBShareLog(), "Facebook ShareDialog was unavailable without a delegate error.");
		KFBClearShareState();
		KFBInvokeShareCallback(callback, KFBShareStatusFailed, @"share_dialog_unavailable", context);
	}
}

KFB_EXPORT bool kfb_facebook_share_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr)
{
	UIApplication *application = (__bridge UIApplication *)applicationPtr;
	NSURL *url = (__bridge NSURL *)urlPtr;
	id appIdValue = NSBundle.mainBundle.infoDictionary[@"FacebookAppID"];
	if (![appIdValue isKindOfClass:NSString.class]) {
		return false;
	}
	NSString *expectedScheme = [@"fb" stringByAppendingString:(NSString *)appIdValue];
	if (url.scheme == nil || [url.scheme caseInsensitiveCompare:expectedScheme] != NSOrderedSame) {
		return false;
	}

	NSDictionary *options = optionsPtr == NULL ? nil : (__bridge NSDictionary *)optionsPtr;
	NSDictionary<UIApplicationOpenURLOptionsKey, id> *openUrlOptions =
		[options isKindOfClass:NSDictionary.class] ? options : @{};

	if (KFBShareDialogInFlight != nil) {
		KFBClearPendingShareDataWithoutReading();
	}

	return [FBSDKApplicationDelegate.sharedInstance application:application
														openURL:url
														options:openUrlOptions];
}
