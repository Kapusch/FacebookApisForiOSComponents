// Runtime lookup of the App Events settings registry.
//
// kfbappevents.xcframework records every setting changed through
// FacebookAppEvents in the Objective-C class KFBAppEventsSettingsRegistry.
// The Login and Share wrappers look it up through the Objective-C runtime, so
// they have no link-time dependency on it:
// - Share does not override a setting the app configured explicitly;
// - both report when they initialized the SDK, which marks it initialized for
//   FacebookAppEvents and applies the recorded values again, so the SDK
//   persists values that were set before initialization.

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

NS_ASSUME_NONNULL_END
