namespace Kapusch.Facebook.iOS;

/// <summary>
/// Standard Meta app event parameter names, as declared by
/// <c>FBSDKAppEventParameterName.h</c> and <c>FBSDKAppEventParameterProduct.h</c> in the
/// bundled Facebook iOS SDK. The names match <c>AppEventsConstants.EVENT_PARAM_*</c> on Android.
/// </summary>
/// <remarks>
/// Each constant names its Objective-C symbol in a <c>FBSDK…</c> remark. The package
/// validation checks that every symbol of the bundled headers is covered and that each
/// value exists in the bundled <c>FBSDKCoreKit</c> binary.
/// </remarks>
public static class FacebookAppEventParameterNames
{
	// General parameters.

	/// <summary>Generic content data. FBSDKAppEventParameterNameContent</summary>
	public const string Content = "fb_content";

	/// <summary>Content or product identifier (for example a SKU). FBSDKAppEventParameterNameContentID</summary>
	public const string ContentId = "fb_content_id";

	/// <summary>Content type, for example <c>product</c>. FBSDKAppEventParameterNameContentType</summary>
	public const string ContentType = "fb_content_type";

	/// <summary>ISO 4217 currency code. FBSDKAppEventParameterNameCurrency</summary>
	public const string Currency = "fb_currency";

	/// <summary>Free-form description. FBSDKAppEventParameterNameDescription</summary>
	public const string Description = "fb_description";

	/// <summary>Level reached. FBSDKAppEventParameterNameLevel</summary>
	public const string Level = "fb_level";

	/// <summary>Upper bound of a rating scale. FBSDKAppEventParameterNameMaxRatingValue</summary>
	public const string MaxRatingValue = "fb_max_rating_value";

	/// <summary>Number of items. FBSDKAppEventParameterNameNumItems</summary>
	public const string NumItems = "fb_num_items";

	/// <summary>Whether payment information is available (<c>1</c>/<c>0</c>). FBSDKAppEventParameterNamePaymentInfoAvailable</summary>
	public const string PaymentInfoAvailable = "fb_payment_info_available";

	/// <summary>Registration method, for example <c>email</c>. FBSDKAppEventParameterNameRegistrationMethod</summary>
	public const string RegistrationMethod = "fb_registration_method";

	/// <summary>Search query. FBSDKAppEventParameterNameSearchString</summary>
	public const string SearchString = "fb_search_string";

	/// <summary>Whether the activity succeeded (<c>1</c>/<c>0</c>). FBSDKAppEventParameterNameSuccess</summary>
	public const string Success = "fb_success";

	/// <summary>Ad type for <see cref="FacebookAppEventNames.AdImpression"/> and <see cref="FacebookAppEventNames.AdClick"/>. FBSDKAppEventParameterNameAdType</summary>
	public const string AdType = "ad_type";

	/// <summary>
	/// Order identifier. For <see cref="FacebookAppEventNames.Subscribe"/> and
	/// <see cref="FacebookAppEventNames.StartTrial"/>, Meta documents it as the unique ID of
	/// the subscription. FBSDKAppEventParameterNameOrderID
	/// </summary>
	public const string OrderId = "fb_order_id";

	/// <summary>
	/// Numeric value summed across events. Pass the <c>valueToSum</c> argument of
	/// <see cref="FacebookAppEvents.LogEvent"/> instead of this key. Internal on iOS,
	/// public on Android (<c>EVENT_PARAM_VALUE_TO_SUM</c>).
	/// </summary>
	public const string ValueToSum = "_valueToSum";

	/// <summary>Event name key used inside the SDK payloads. FBSDKAppEventParameterNameEventName</summary>
	public const string EventName = "_eventName";

	/// <summary>Event time key used inside the SDK payloads. FBSDKAppEventParameterNameLogTime</summary>
	public const string LogTime = "_logTime";

	// In-app purchase parameters set by the SDK's automatic purchase logging.

	/// <summary>Marks an automatically logged purchase. FBSDKAppEventParameterNameImplicitlyLoggedPurchase</summary>
	public const string ImplicitlyLoggedPurchase = "_implicitlyLogged";

	/// <summary>Transaction date. FBSDKAppEventParameterNameTransactionDate</summary>
	public const string TransactionDate = "fb_transaction_date";

	/// <summary>Product title. FBSDKAppEventParameterNameProductTitle</summary>
	public const string ProductTitle = "fb_content_title";

	/// <summary>Transaction identifier. FBSDKAppEventParameterNameTransactionID</summary>
	public const string TransactionId = "fb_transaction_id";

	/// <summary>In-app purchase product type. FBSDKAppEventParameterNameInAppPurchaseType</summary>
	public const string InAppPurchaseType = "fb_iap_product_type";

	/// <summary>Subscription period. FBSDKAppEventParameterNameSubscriptionPeriod</summary>
	public const string SubscriptionPeriod = "fb_iap_subs_period";

	/// <summary>Whether the purchase starts a trial. FBSDKAppEventParameterNameIsStartTrial</summary>
	public const string IsStartTrial = "fb_iap_is_start_trial";

	/// <summary>Whether the product has a free trial. FBSDKAppEventParameterNameHasFreeTrial</summary>
	public const string HasFreeTrial = "fb_iap_has_free_trial";

	/// <summary>Trial period. FBSDKAppEventParameterNameTrialPeriod</summary>
	public const string TrialPeriod = "fb_iap_trial_period";

	/// <summary>Trial price. FBSDKAppEventParameterNameTrialPrice</summary>
	public const string TrialPrice = "fb_iap_trial_price";

	/// <summary>Receipt validation result. FBSDKAppEventParameterNameValidationResult</summary>
	public const string ValidationResult = "fb_iap_validation_result";

	/// <summary>StoreKit client library version. FBSDKAppEventParameterNameIAPClientLibraryVersion</summary>
	public const string IapClientLibraryVersion = "fb_iap_client_library_version";

	/// <summary>StoreKit library versions supported by the SDK. FBSDKAppEventParameterNameIAPSDKLibraryVersions</summary>
	public const string IapSdkLibraryVersions = "fb_iap_sdk_supported_library_versions";

	/// <summary>Receipt data. FBSDKAppEventParameterNameIAPReceiptData</summary>
	public const string IapReceiptData = "receipt_data";

	/// <summary>Product classification. FBSDKAppEventParameterNameProductClassification</summary>
	public const string ProductClassification = "fb_iap_product_classification";

	/// <summary>Whether consumables are in the purchase history. FBSDKAppEventParameterNameConsumablesInPurchaseHistory</summary>
	public const string ConsumablesInPurchaseHistory = "fb_iap_consumables_in_purchase_history";

	/// <summary>Original transaction identifier. FBSDKAppEventParameterNameOriginalTransactionID</summary>
	public const string OriginalTransactionId = "fb_original_transaction_id";

	/// <summary>Original transaction date. FBSDKAppEventParameterNameOriginalTransactionDate</summary>
	public const string OriginalTransactionDate = "fb_original_transaction_date";

	// Product catalog parameters.

	/// <summary>Product category. FBSDKAppEventParameterProductCategory</summary>
	public const string ProductCategory = "fb_product_category";

	/// <summary>Product custom label 0. FBSDKAppEventParameterProductCustomLabel0</summary>
	public const string ProductCustomLabel0 = "fb_product_custom_label_0";

	/// <summary>Product custom label 1. FBSDKAppEventParameterProductCustomLabel1</summary>
	public const string ProductCustomLabel1 = "fb_product_custom_label_1";

	/// <summary>Product custom label 2. FBSDKAppEventParameterProductCustomLabel2</summary>
	public const string ProductCustomLabel2 = "fb_product_custom_label_2";

	/// <summary>Product custom label 3. FBSDKAppEventParameterProductCustomLabel3</summary>
	public const string ProductCustomLabel3 = "fb_product_custom_label_3";

	/// <summary>Product custom label 4. FBSDKAppEventParameterProductCustomLabel4</summary>
	public const string ProductCustomLabel4 = "fb_product_custom_label_4";

	/// <summary>Product App Link iOS URL. FBSDKAppEventParameterProductAppLinkIOSUrl</summary>
	public const string ProductAppLinkIosUrl = "fb_product_applink_ios_url";

	/// <summary>Product App Link iOS App Store ID. FBSDKAppEventParameterProductAppLinkIOSAppStoreID</summary>
	public const string ProductAppLinkIosAppStoreId = "fb_product_applink_ios_app_store_id";

	/// <summary>Product App Link iOS app name. FBSDKAppEventParameterProductAppLinkIOSAppName</summary>
	public const string ProductAppLinkIosAppName = "fb_product_applink_ios_app_name";

	/// <summary>Product App Link iPhone URL. FBSDKAppEventParameterProductAppLinkIPhoneUrl</summary>
	public const string ProductAppLinkIPhoneUrl = "fb_product_applink_iphone_url";

	/// <summary>Product App Link iPhone App Store ID. FBSDKAppEventParameterProductAppLinkIPhoneAppStoreID</summary>
	public const string ProductAppLinkIPhoneAppStoreId = "fb_product_applink_iphone_app_store_id";

	/// <summary>Product App Link iPhone app name. FBSDKAppEventParameterProductAppLinkIPhoneAppName</summary>
	public const string ProductAppLinkIPhoneAppName = "fb_product_applink_iphone_app_name";

	/// <summary>Product App Link iPad URL. FBSDKAppEventParameterProductAppLinkIPadUrl</summary>
	public const string ProductAppLinkIPadUrl = "fb_product_applink_ipad_url";

	/// <summary>Product App Link iPad App Store ID. FBSDKAppEventParameterProductAppLinkIPadAppStoreID</summary>
	public const string ProductAppLinkIPadAppStoreId = "fb_product_applink_ipad_app_store_id";

	/// <summary>Product App Link iPad app name. FBSDKAppEventParameterProductAppLinkIPadAppName</summary>
	public const string ProductAppLinkIPadAppName = "fb_product_applink_ipad_app_name";

	/// <summary>Product App Link Android URL. FBSDKAppEventParameterProductAppLinkAndroidUrl</summary>
	public const string ProductAppLinkAndroidUrl = "fb_product_applink_android_url";

	/// <summary>Product App Link Android package. FBSDKAppEventParameterProductAppLinkAndroidPackage</summary>
	public const string ProductAppLinkAndroidPackage = "fb_product_applink_android_package";

	/// <summary>Product App Link Android app name. FBSDKAppEventParameterProductAppLinkAndroidAppName</summary>
	public const string ProductAppLinkAndroidAppName = "fb_product_applink_android_app_name";

	/// <summary>Product App Link Windows Phone URL. FBSDKAppEventParameterProductAppLinkWindowsPhoneUrl</summary>
	public const string ProductAppLinkWindowsPhoneUrl = "fb_product_applink_windows_phone_url";

	/// <summary>Product App Link Windows Phone app ID. FBSDKAppEventParameterProductAppLinkWindowsPhoneAppID</summary>
	public const string ProductAppLinkWindowsPhoneAppId = "fb_product_applink_windows_phone_app_id";

	/// <summary>Product App Link Windows Phone app name. FBSDKAppEventParameterProductAppLinkWindowsPhoneAppName</summary>
	public const string ProductAppLinkWindowsPhoneAppName = "fb_product_applink_windows_phone_app_name";
}

/// <summary>
/// Standard parameter values (<c>FBSDKAppEventParameterValue.h</c>). Boolean parameter
/// values passed to <see cref="FacebookAppEvents"/> are converted to these strings.
/// </summary>
public static class FacebookAppEventParameterValues
{
	/// <summary>Yes / true. FBSDKAppEventParameterValueYes</summary>
	public const string Yes = "1";

	/// <summary>No / false. FBSDKAppEventParameterValueNo</summary>
	public const string No = "0";
}

/// <summary>
/// Advanced-matching user data types (<c>FBSDKAppEventUserDataType.h</c>), for
/// <see cref="FacebookAppEvents.SetUserDataValue"/>. The SDK hashes values before sending them.
/// </summary>
public static class FacebookAppEventUserDataTypes
{
	/// <summary>Email. FBSDKAppEventEmail</summary>
	public const string Email = "em";

	/// <summary>First name. FBSDKAppEventFirstName</summary>
	public const string FirstName = "fn";

	/// <summary>Last name. FBSDKAppEventLastName</summary>
	public const string LastName = "ln";

	/// <summary>Phone number. FBSDKAppEventPhone</summary>
	public const string Phone = "ph";

	/// <summary>Date of birth (<c>YYYYMMDD</c>). FBSDKAppEventDateOfBirth</summary>
	public const string DateOfBirth = "dob";

	/// <summary>Gender (<c>m</c> or <c>f</c>). FBSDKAppEventGender</summary>
	public const string Gender = "ge";

	/// <summary>City. FBSDKAppEventCity</summary>
	public const string City = "ct";

	/// <summary>State or province. FBSDKAppEventState</summary>
	public const string State = "st";

	/// <summary>Zip or postal code. FBSDKAppEventZip</summary>
	public const string Zip = "zp";

	/// <summary>Country (ISO 3166-1 alpha-2). FBSDKAppEventCountry</summary>
	public const string Country = "country";

	/// <summary>Advertiser-side user identifier. iOS only in the native SDK API. FBSDKAppEventExternalId</summary>
	public const string ExternalId = "external_id";
}

/// <summary>Data processing options for <see cref="FacebookAppEvents.SetDataProcessingOptions"/>.</summary>
public static class FacebookDataProcessingOptions
{
	/// <summary>Limited Data Use. Pass an empty array to explicitly not enable it.</summary>
	public const string LimitedDataUse = "LDU";
}
