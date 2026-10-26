// C interop surface of the Facebook App Events static library
// (kfbappevents.xcframework). It only uses FBSDKCoreKit, which every feature
// selection already links.
//
// Strings returned by kfb_appevents_copy_* are allocated with malloc and must
// be released with kfb_appevents_free. Event parameters are passed as a UTF-8
// JSON object whose values are strings or numbers.

#pragma once

#include "KapuschFacebookInteropExport.h"

// SDK lifecycle.
KFB_EXPORT void kfb_appevents_initialize_sdk(void);
KFB_EXPORT bool kfb_appevents_finished_launching(void *applicationPtr, void *launchOptionsPtr);
KFB_EXPORT bool kfb_appevents_handle_open_url(void *applicationPtr, void *urlPtr, void *optionsPtr);
KFB_EXPORT bool kfb_appevents_continue_user_activity(void *applicationPtr, void *userActivityPtr);
KFB_EXPORT const char *kfb_appevents_copy_sdk_version(void);

// Settings (persisted by the SDK across launches).
KFB_EXPORT void kfb_appevents_set_auto_log_app_events_enabled(bool enabled);
KFB_EXPORT bool kfb_appevents_is_auto_log_app_events_enabled(void);
KFB_EXPORT void kfb_appevents_set_advertiser_id_collection_enabled(bool enabled);
KFB_EXPORT bool kfb_appevents_is_advertiser_id_collection_enabled(void);
KFB_EXPORT void kfb_appevents_set_advertiser_tracking_enabled(bool enabled);
KFB_EXPORT bool kfb_appevents_is_advertiser_tracking_enabled(void);
KFB_EXPORT void kfb_appevents_set_skadnetwork_report_enabled(bool enabled);
KFB_EXPORT bool kfb_appevents_is_skadnetwork_report_enabled(void);
KFB_EXPORT void kfb_appevents_set_metadata_collection_enabled(bool enabled);
KFB_EXPORT bool kfb_appevents_is_metadata_collection_enabled(void);
KFB_EXPORT void kfb_appevents_set_auto_log_product_improvement_enabled(bool enabled);
KFB_EXPORT bool kfb_appevents_is_auto_log_product_improvement_enabled(void);
KFB_EXPORT void kfb_appevents_set_data_processing_options(const char *optionsJson, int32_t country, int32_t state);
KFB_EXPORT void kfb_appevents_set_debug_logging_enabled(bool enabled);

// Event logging.
KFB_EXPORT void kfb_appevents_activate_app(void);
KFB_EXPORT bool kfb_appevents_log_event(const char *eventName, const char *parametersJson, bool hasValueToSum, double valueToSum);
KFB_EXPORT bool kfb_appevents_log_purchase(double amount, const char *currency, const char *parametersJson);
KFB_EXPORT void kfb_appevents_flush(void);
KFB_EXPORT void kfb_appevents_set_flush_behavior(int32_t behavior);
KFB_EXPORT int32_t kfb_appevents_get_flush_behavior(void);

// Identity and advanced matching (the SDK hashes user data before sending it).
KFB_EXPORT const char *kfb_appevents_copy_anonymous_id(void);
KFB_EXPORT void kfb_appevents_set_user_id(const char *userId);
KFB_EXPORT const char *kfb_appevents_copy_user_id(void);
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
	const char *country);
KFB_EXPORT void kfb_appevents_set_user_data_for_type(const char *type, const char *value);
KFB_EXPORT void kfb_appevents_clear_user_data(void);
KFB_EXPORT void kfb_appevents_clear_user_data_for_type(const char *type);

KFB_EXPORT void kfb_appevents_free(void *pointer);
