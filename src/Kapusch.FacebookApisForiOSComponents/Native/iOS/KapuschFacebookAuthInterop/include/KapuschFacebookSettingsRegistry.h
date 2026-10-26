// Runtime lookup of the App Events settings registry.
//
// kfbappevents.xcframework records every setting changed through
// FacebookAppEvents in the Objective-C class KFBAppEventsSettingsRegistry.
// The Login and Share wrappers look it up through the Objective-C runtime, so
// they have no link-time dependency on it:
// - Share does not override a setting the app configured explicitly;
// - both apply the recorded values again after initializing the SDK, so the
//   SDK persists values that were set before initialization.

#pragma once

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

static NSString *const KFBRegistrySettingAutoLogAppEvents = @"autoLogAppEvents";
static NSString *const KFBRegistrySettingAdvertiserIDCollection = @"advertiserIDCollection";
static NSString *const KFBRegistrySettingAdvertiserTracking = @"advertiserTracking";

@protocol KFBAppEventsSettingsRegistryLookup <NSObject>
+ (nullable NSNumber *)explicitValueForSetting:(NSString *)setting;
+ (void)applyExplicitValues;
@end

static inline Class<KFBAppEventsSettingsRegistryLookup> _Nullable KFBAppEventsSettingsRegistryClass(void)
{
	Class registry = NSClassFromString(@"KFBAppEventsSettingsRegistry");
	if (registry == Nil
		|| ![registry respondsToSelector:@selector(explicitValueForSetting:)]
		|| ![registry respondsToSelector:@selector(applyExplicitValues)]) {
		return Nil;
	}
	return (Class<KFBAppEventsSettingsRegistryLookup>)registry;
}

static inline NSNumber *_Nullable KFBExplicitAppEventsSetting(NSString *setting)
{
	return [KFBAppEventsSettingsRegistryClass() explicitValueForSetting:setting];
}

static inline void KFBApplyExplicitAppEventsSettings(void)
{
	[KFBAppEventsSettingsRegistryClass() applyExplicitValues];
}

NS_ASSUME_NONNULL_END
