// Runtime lookup of the App Events settings registry.
//
// kfbappevents.xcframework records every setting changed through
// FacebookAppEvents in the Objective-C class KFBAppEventsSettingsRegistry.
// The Login and Share wrappers look it up through the Objective-C runtime, so
// they have no link-time dependency on it:
// - Share does not override a setting the app configured explicitly;
// - both report when they initialized the SDK, which marks it initialized for
//   FacebookAppEvents and applies the recorded values again, so the SDK
//   persists values that were set before initialization;
// - Login initializes the SDK through the App Events initialization itself
//   (KFBAppEventsFinishLaunching), so Login and FacebookAppEvents share one
//   initialization routine.

#pragma once

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

static NSString *const KFBRegistrySettingAutoLogAppEvents = @"autoLogAppEvents";
static NSString *const KFBRegistrySettingAdvertiserIDCollection = @"advertiserIDCollection";
static NSString *const KFBRegistrySettingAdvertiserTracking = @"advertiserTracking";

@protocol KFBAppEventsSettingsRegistryLookup <NSObject>
+ (nullable NSNumber *)explicitValueForSetting:(NSString *)setting;
+ (void)sdkDidInitialize;
@end

@protocol KFBAppEventsInitializationLookup <NSObject>
+ (BOOL)finishLaunchingApplication:(id)application options:(nullable NSDictionary *)launchOptions;
@end

static inline Class<KFBAppEventsSettingsRegistryLookup> _Nullable KFBAppEventsSettingsRegistryClass(void)
{
	Class registry = NSClassFromString(@"KFBAppEventsSettingsRegistry");
	if (registry == Nil
		|| ![registry respondsToSelector:@selector(explicitValueForSetting:)]
		|| ![registry respondsToSelector:@selector(sdkDidInitialize)]) {
		return Nil;
	}
	return (Class<KFBAppEventsSettingsRegistryLookup>)registry;
}

static inline NSNumber *_Nullable KFBExplicitAppEventsSetting(NSString *setting)
{
	return [KFBAppEventsSettingsRegistryClass() explicitValueForSetting:setting];
}

// Call right after FBSDKApplicationDelegate application:didFinishLaunchingWithOptions:.
static inline void KFBNotifyAppEventsSdkInitialized(void)
{
	[KFBAppEventsSettingsRegistryClass() sdkDidInitialize];
}

// Initializes the SDK exactly like FacebookAppEvents.FinishedLaunching
// (application:didFinishLaunchingWithOptions:, then the explicit settings are
// applied again). Main thread only. Returns NO without initializing when the
// App Events facade is not linked, which the package targets never allow.
static inline BOOL KFBAppEventsFinishLaunching(id application, NSDictionary *_Nullable launchOptions, BOOL *_Nullable available)
{
	Class registry = KFBAppEventsSettingsRegistryClass();
	BOOL found = registry != Nil
		&& [registry respondsToSelector:@selector(finishLaunchingApplication:options:)];
	if (available != NULL) {
		*available = found;
	}
	if (!found) {
		return NO;
	}
	return [(Class<KFBAppEventsInitializationLookup>)registry finishLaunchingApplication:application
																				 options:launchOptions];
}

NS_ASSUME_NONNULL_END
