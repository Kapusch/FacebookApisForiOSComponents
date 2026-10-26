// Facebook App Events interop exported to .NET through C symbols.
//
// Thin facade over FBSDKAppEvents / FBSDKSettings / FBSDKApplicationDelegate,
// written against the SDK's Objective-C surface. It never changes a setting on
// its own: every value is set explicitly by the consumer.

@import Foundation;
@import os.log;
@import UIKit;
@import FBSDKCoreKit;

#include <stdlib.h>
#include <string.h>

#include "KapuschFacebookAppEventsInterop.h"
#include "KapuschFacebookSettingsRegistry.h"

static os_log_t KFBAppEventsLog(void)
{
	static os_log_t log;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		NSString *subsystem = NSBundle.mainBundle.bundleIdentifier ?: @"Kapusch.Facebook.iOS";
		log = os_log_create(subsystem.UTF8String, "FacebookAppEvents");
	});
	return log;
}

#pragma mark - Explicit settings registry

// Settings changed through this facade, by name, with their last value.
//
// - The values are applied again right after the SDK is initialized. The SDK
//   keeps a value set before initialization in memory only; re-applying it once
//   the SDK is configured lets the SDK persist it, like any later change.
// - Other wrappers in this package look the registry up through the
//   Objective-C runtime (no link-time dependency) so they do not override a
//   setting the app configured explicitly.
//
// The class name, selectors and setting names are a contract with
// KapuschFacebookSettingsRegistry.h; keep them in sync.
@interface KFBAppEventsSettingsRegistry : NSObject
+ (void)recordValue:(BOOL)value forSetting:(NSString *)setting;
+ (nullable NSNumber *)explicitValueForSetting:(NSString *)setting;
+ (NSDictionary<NSString *, NSNumber *> *)explicitValues;
+ (void)applyExplicitValues;
@end

static void KFBReapplyExplicitSettings(void);

@implementation KFBAppEventsSettingsRegistry

+ (NSMutableDictionary<NSString *, NSNumber *> *)storage
{
	static NSMutableDictionary<NSString *, NSNumber *> *storage;
	static dispatch_once_t onceToken;
	dispatch_once(&onceToken, ^{
		storage = [NSMutableDictionary dictionary];
	});
	return storage;
}

+ (void)recordValue:(BOOL)value forSetting:(NSString *)setting
{
	@synchronized(self) {
		[self storage][setting] = @(value);
	}
}

+ (nullable NSNumber *)explicitValueForSetting:(NSString *)setting
{
	@synchronized(self) {
		return [self storage][setting];
	}
}

+ (NSDictionary<NSString *, NSNumber *> *)explicitValues
{
	@synchronized(self) {
		return [[self storage] copy];
	}
}

+ (void)applyExplicitValues
{
	KFBReapplyExplicitSettings();
}

@end

// Settings shared with the Login and Share wrappers come from KapuschFacebookSettingsRegistry.h.
#define KFBSettingAutoLogAppEvents KFBRegistrySettingAutoLogAppEvents
#define KFBSettingAdvertiserIDCollection KFBRegistrySettingAdvertiserIDCollection
#define KFBSettingAdvertiserTracking KFBRegistrySettingAdvertiserTracking
static NSString *const KFBSettingSKAdNetworkReport = @"skAdNetworkReport";
static NSString *const KFBSettingMetadataCollection = @"metadataCollection";
static NSString *const KFBSettingAutoLogProductImprovement = @"autoLogProductImprovement";

static void KFBApplySetting(NSString *setting, BOOL value)
{
	FBSDKSettings *settings = FBSDKSettings.sharedSettings;
	if ([setting isEqualToString:KFBSettingAutoLogAppEvents]) {
		settings.isAutoLogAppEventsEnabled = value;
	} else if ([setting isEqualToString:KFBSettingAdvertiserIDCollection]) {
		settings.isAdvertiserIDCollectionEnabled = value;
	} else if ([setting isEqualToString:KFBSettingAdvertiserTracking]) {
		// Deprecated by Meta on iOS 17+, where the SDK reads the App Tracking
		// Transparency status instead and ignores this value.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
		settings.isAdvertiserTrackingEnabled = value;
#pragma clang diagnostic pop
	} else if ([setting isEqualToString:KFBSettingSKAdNetworkReport]) {
		settings.isSKAdNetworkReportEnabled = value;
	} else if ([setting isEqualToString:KFBSettingMetadataCollection]) {
		settings.isMetaDataCollectionEnabled = value;
	} else if ([setting isEqualToString:KFBSettingAutoLogProductImprovement]) {
		settings.isAutoLogProductImprovementEnabled = value;
	}
}

static void KFBSetExplicitSetting(NSString *setting, bool value)
{
	[KFBAppEventsSettingsRegistry recordValue:value forSetting:setting];
	KFBApplySetting(setting, value);
}

static void KFBReapplyExplicitSettings(void)
{
	[[KFBAppEventsSettingsRegistry explicitValues]
		enumerateKeysAndObjectsUsingBlock:^(NSString *setting, NSNumber *value, BOOL *stop) {
			KFBApplySetting(setting, value.boolValue);
		}];
}

#pragma mark - Helpers

static void KFBRunOnMainThread(dispatch_block_t block)
{
	if (NSThread.isMainThread) {
		block();
	} else {
		dispatch_async(dispatch_get_main_queue(), block);
	}
}

static NSString *KFBStringOrNil(const char *value)
{
	return value == NULL ? nil : [NSString stringWithUTF8String:value];
}

static const char *KFBCopyString(NSString *value)
{
	if (value == nil) {
		return NULL;
	}
	const char *utf8 = value.UTF8String;
	return utf8 == NULL ? NULL : strdup(utf8);
}

static id KFBParseJson(const char *json)
{
	if (json == NULL) {
		return nil;
	}
	NSData *data = [NSData dataWithBytes:json length:strlen(json)];
	NSError *error = nil;
	id value = [NSJSONSerialization JSONObjectWithData:data options:0 error:&error];
	if (value == nil) {
		os_log_error(
			KFBAppEventsLog(),
			"Invalid JSON payload from managed code: %{public}@",
			error.localizedDescription);
	}
	return value;
}

// Parses event parameters. Returns NO when the payload is not a JSON object of
// string or number values; *parameters is nil when no payload was given.
static BOOL KFBParseParameters(const char *json, NSDictionary<FBSDKAppEventParameterName, id> **parameters)
{
	*parameters = nil;
	if (json == NULL) {
		return YES;
	}

	id value = KFBParseJson(json);
	if (![value isKindOfClass:NSDictionary.class]) {
		return NO;
	}

	__block BOOL valid = YES;
	[(NSDictionary *)value enumerateKeysAndObjectsUsingBlock:^(id key, id object, BOOL *stop) {
		if (![key isKindOfClass:NSString.class]
			|| !([object isKindOfClass:NSString.class] || [object isKindOfClass:NSNumber.class])) {
			valid = NO;
			*stop = YES;
		}
	}];
	if (!valid) {
		os_log_error(KFBAppEventsLog(), "App event parameters must map strings to strings or numbers.");
		return NO;
	}

	*parameters = (NSDictionary *)value;
	return YES;
}

static NSDictionary *KFBOpenUrlOptions(void *optionsPtr)
{
	NSDictionary *options = optionsPtr == NULL ? nil : (__bridge NSDictionary *)optionsPtr;
	return [options isKindOfClass:NSDictionary.class] ? options : @{};
}

#pragma mark - SDK lifecycle

static BOOL KFBFinishLaunching(UIApplication *application, NSDictionary *launchOptions)
{
	BOOL result = [FBSDKApplicationDelegate.sharedInstance application:application
										 didFinishLaunchingWithOptions:launchOptions];
	KFBReapplyExplicitSettings();
	return result;
}

KFB_EXPORT void kfb_appevents_initialize_sdk(void)
{
	KFBRunOnMainThread(^{
		KFBFinishLaunching(UIApplication.sharedApplication, nil);
	});
}

KFB_EXPORT bool kfb_appevents_finished_launching(void *applicationPtr, void *launchOptionsPtr)
{
	UIApplication *application = (__bridge UIApplication *)applicationPtr;
	NSDictionary *launchOptions = launchOptionsPtr == NULL
		? nil
		: (__bridge NSDictionary *)launchOptionsPtr;
	return KFBFinishLaunching(application, launchOptions);
}

KFB_EXPORT bool kfb_appevents_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr)
{
	UIApplication *application = (__bridge UIApplication *)applicationPtr;
	NSURL *url = (__bridge NSURL *)urlPtr;
	return [FBSDKApplicationDelegate.sharedInstance application:application
														openURL:url
														options:KFBOpenUrlOptions(optionsPtr)];
}

KFB_EXPORT bool kfb_appevents_continue_user_activity(void *applicationPtr, void *userActivityPtr)
{
	UIApplication *application = (__bridge UIApplication *)applicationPtr;
	NSUserActivity *userActivity = (__bridge NSUserActivity *)userActivityPtr;
	return [FBSDKApplicationDelegate.sharedInstance application:application
										   continueUserActivity:userActivity];
}

KFB_EXPORT const char *kfb_appevents_copy_sdk_version(void)
{
	return KFBCopyString(FBSDKSettings.sharedSettings.sdkVersion);
}

#pragma mark - Settings

KFB_EXPORT void kfb_appevents_set_auto_log_app_events_enabled(bool enabled)
{
	KFBSetExplicitSetting(KFBSettingAutoLogAppEvents, enabled);
}

KFB_EXPORT bool kfb_appevents_is_auto_log_app_events_enabled(void)
{
	return FBSDKSettings.sharedSettings.isAutoLogAppEventsEnabled;
}

KFB_EXPORT void kfb_appevents_set_advertiser_id_collection_enabled(bool enabled)
{
	KFBSetExplicitSetting(KFBSettingAdvertiserIDCollection, enabled);
}

KFB_EXPORT bool kfb_appevents_is_advertiser_id_collection_enabled(void)
{
	return FBSDKSettings.sharedSettings.isAdvertiserIDCollectionEnabled;
}

KFB_EXPORT void kfb_appevents_set_advertiser_tracking_enabled(bool enabled)
{
	KFBSetExplicitSetting(KFBSettingAdvertiserTracking, enabled);
}

KFB_EXPORT bool kfb_appevents_is_advertiser_tracking_enabled(void)
{
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"
	return FBSDKSettings.sharedSettings.isAdvertiserTrackingEnabled;
#pragma clang diagnostic pop
}

KFB_EXPORT void kfb_appevents_set_skadnetwork_report_enabled(bool enabled)
{
	KFBSetExplicitSetting(KFBSettingSKAdNetworkReport, enabled);
}

KFB_EXPORT bool kfb_appevents_is_skadnetwork_report_enabled(void)
{
	return FBSDKSettings.sharedSettings.isSKAdNetworkReportEnabled;
}

KFB_EXPORT void kfb_appevents_set_metadata_collection_enabled(bool enabled)
{
	KFBSetExplicitSetting(KFBSettingMetadataCollection, enabled);
}

KFB_EXPORT bool kfb_appevents_is_metadata_collection_enabled(void)
{
	return FBSDKSettings.sharedSettings.isMetaDataCollectionEnabled;
}

KFB_EXPORT void kfb_appevents_set_auto_log_product_improvement_enabled(bool enabled)
{
	KFBSetExplicitSetting(KFBSettingAutoLogProductImprovement, enabled);
}

KFB_EXPORT bool kfb_appevents_is_auto_log_product_improvement_enabled(void)
{
	return FBSDKSettings.sharedSettings.isAutoLogProductImprovementEnabled;
}

KFB_EXPORT void kfb_appevents_set_data_processing_options(const char *optionsJson, int32_t country, int32_t state)
{
	id value = KFBParseJson(optionsJson);
	if (![value isKindOfClass:NSArray.class]) {
		os_log_error(KFBAppEventsLog(), "Data processing options must be a JSON array of strings.");
		return;
	}
	for (id option in (NSArray *)value) {
		if (![option isKindOfClass:NSString.class]) {
			os_log_error(KFBAppEventsLog(), "Data processing options must be a JSON array of strings.");
			return;
		}
	}
	[FBSDKSettings.sharedSettings setDataProcessingOptions:(NSArray<NSString *> *)value
												   country:country
													 state:state];
}

static NSSet<FBSDKLoggingBehavior> *KFBDebugLoggingBehaviors(void)
{
	return [NSSet setWithObjects:
		FBSDKLoggingBehaviorAppEvents,
		FBSDKLoggingBehaviorDeveloperErrors,
		FBSDKLoggingBehaviorInformational,
		nil];
}

KFB_EXPORT void kfb_appevents_set_debug_logging_enabled(bool enabled)
{
	// Remember the behaviours that were active before enabling diagnostics, so
	// disabling them restores the SDK defaults instead of silencing everything.
	static NSSet<FBSDKLoggingBehavior> *previousBehaviors = nil;
	@synchronized(KFBAppEventsSettingsRegistry.class) {
		FBSDKSettings *settings = FBSDKSettings.sharedSettings;
		if (enabled) {
			if (previousBehaviors == nil) {
				previousBehaviors = [settings.loggingBehaviors copy];
			}
			settings.loggingBehaviors = [settings.loggingBehaviors setByAddingObjectsFromSet:KFBDebugLoggingBehaviors()];
		} else if (previousBehaviors != nil) {
			settings.loggingBehaviors = previousBehaviors;
			previousBehaviors = nil;
		}
	}
}

#pragma mark - Event logging

KFB_EXPORT void kfb_appevents_activate_app(void)
{
	KFBRunOnMainThread(^{
		[FBSDKAppEvents.shared activateApp];
	});
}

KFB_EXPORT bool kfb_appevents_log_event(const char *eventName, const char *parametersJson, bool hasValueToSum, double valueToSum)
{
	NSString *name = KFBStringOrNil(eventName);
	NSDictionary<FBSDKAppEventParameterName, id> *parameters = nil;
	if (name.length == 0 || !KFBParseParameters(parametersJson, &parameters)) {
		return false;
	}

	if (hasValueToSum) {
		[FBSDKAppEvents.shared logEvent:name valueToSum:valueToSum parameters:parameters];
	} else {
		[FBSDKAppEvents.shared logEvent:name parameters:parameters];
	}
	return true;
}

KFB_EXPORT bool kfb_appevents_log_purchase(double amount, const char *currency, const char *parametersJson)
{
	NSString *currencyCode = KFBStringOrNil(currency);
	NSDictionary<FBSDKAppEventParameterName, id> *parameters = nil;
	if (currencyCode.length == 0 || !KFBParseParameters(parametersJson, &parameters)) {
		return false;
	}

	[FBSDKAppEvents.shared logPurchase:amount currency:currencyCode parameters:parameters];
	return true;
}

KFB_EXPORT void kfb_appevents_flush(void)
{
	[FBSDKAppEvents.shared flush];
}

KFB_EXPORT void kfb_appevents_set_flush_behavior(int32_t behavior)
{
	FBSDKAppEvents.shared.flushBehavior = behavior == (int32_t)FBSDKAppEventsFlushBehaviorExplicitOnly
		? FBSDKAppEventsFlushBehaviorExplicitOnly
		: FBSDKAppEventsFlushBehaviorAuto;
}

KFB_EXPORT int32_t kfb_appevents_get_flush_behavior(void)
{
	return (int32_t)FBSDKAppEvents.shared.flushBehavior;
}

#pragma mark - Identity and advanced matching

KFB_EXPORT const char *kfb_appevents_copy_anonymous_id(void)
{
	return KFBCopyString(FBSDKAppEvents.shared.anonymousID);
}

KFB_EXPORT void kfb_appevents_set_user_id(const char *userId)
{
	NSString *value = KFBStringOrNil(userId);
	FBSDKAppEvents.shared.userID = value.length == 0 ? nil : value;
}

KFB_EXPORT const char *kfb_appevents_copy_user_id(void)
{
	return KFBCopyString(FBSDKAppEvents.shared.userID);
}

KFB_EXPORT void kfb_appevents_set_user_data(
	const char *email,
	const char *firstName,
	const char *lastName,
	const char *phone,
	const char *dateOfBirth,
	const char *gender,
	const char *city,
	const char *state,
	const char *zip,
	const char *country)
{
	[FBSDKAppEvents.shared setUserEmail:KFBStringOrNil(email)
							  firstName:KFBStringOrNil(firstName)
							   lastName:KFBStringOrNil(lastName)
								  phone:KFBStringOrNil(phone)
							dateOfBirth:KFBStringOrNil(dateOfBirth)
								 gender:KFBStringOrNil(gender)
								   city:KFBStringOrNil(city)
								  state:KFBStringOrNil(state)
									zip:KFBStringOrNil(zip)
								country:KFBStringOrNil(country)];
}

KFB_EXPORT void kfb_appevents_set_user_data_for_type(const char *type, const char *value)
{
	NSString *dataType = KFBStringOrNil(type);
	if (dataType.length == 0) {
		return;
	}
	[FBSDKAppEvents.shared setUserData:KFBStringOrNil(value) forType:dataType];
}

KFB_EXPORT void kfb_appevents_clear_user_data(void)
{
	[FBSDKAppEvents.shared clearUserData];
}

KFB_EXPORT void kfb_appevents_clear_user_data_for_type(const char *type)
{
	NSString *dataType = KFBStringOrNil(type);
	if (dataType.length == 0) {
		return;
	}
	[FBSDKAppEvents.shared clearUserDataForType:dataType];
}

KFB_EXPORT void kfb_appevents_free(void *pointer)
{
	free(pointer);
}
