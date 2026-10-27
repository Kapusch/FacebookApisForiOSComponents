namespace Kapusch.Facebook.iOS;

/// <summary>
/// Standard Meta app event names, as declared by <c>FBSDKAppEventName.h</c> in the
/// bundled Facebook iOS SDK (values from <c>FBSDKAppEventName.m</c>). The names match
/// <c>AppEventsConstants.EVENT_NAME_*</c> on Android.
/// </summary>
/// <remarks>
/// Each constant names its Objective-C symbol in a <c>FBSDK…</c> remark. The package
/// validation checks that every symbol of the bundled header is covered and that each
/// value exists in the bundled <c>FBSDKCoreKit</c> binary.
/// </remarks>
public static class FacebookAppEventNames
{
	// General purpose.

	/// <summary>Ad click (ad-supported apps). FBSDKAppEventNameAdClick</summary>
	public const string AdClick = "AdClick";

	/// <summary>Ad impression (ad-supported apps). FBSDKAppEventNameAdImpression</summary>
	public const string AdImpression = "AdImpression";

	/// <summary>The user completed a registration. FBSDKAppEventNameCompletedRegistration</summary>
	public const string CompletedRegistration = "fb_mobile_complete_registration";

	/// <summary>The user completed a tutorial. FBSDKAppEventNameCompletedTutorial</summary>
	public const string CompletedTutorial = "fb_mobile_tutorial_completion";

	/// <summary>The user contacted the business. FBSDKAppEventNameContact</summary>
	public const string Contact = "Contact";

	/// <summary>The user customized a product. FBSDKAppEventNameCustomizeProduct</summary>
	public const string CustomizeProduct = "CustomizeProduct";

	/// <summary>The user donated. FBSDKAppEventNameDonate</summary>
	public const string Donate = "Donate";

	/// <summary>The user searched for a location. FBSDKAppEventNameFindLocation</summary>
	public const string FindLocation = "FindLocation";

	/// <summary>The user rated an item. FBSDKAppEventNameRated</summary>
	public const string Rated = "fb_mobile_rate";

	/// <summary>The user booked an appointment. FBSDKAppEventNameSchedule</summary>
	public const string Schedule = "Schedule";

	/// <summary>The user performed a search. FBSDKAppEventNameSearched</summary>
	public const string Searched = "fb_mobile_search";

	/// <summary>The user started a free trial. FBSDKAppEventNameStartTrial</summary>
	public const string StartTrial = "StartTrial";

	/// <summary>The user submitted an application. FBSDKAppEventNameSubmitApplication</summary>
	public const string SubmitApplication = "SubmitApplication";

	/// <summary>The user started a paid subscription. FBSDKAppEventNameSubscribe</summary>
	public const string Subscribe = "Subscribe";

	/// <summary>The user viewed content. FBSDKAppEventNameViewedContent</summary>
	public const string ViewedContent = "fb_mobile_content_view";

	// E-commerce.

	/// <summary>The user added payment information. FBSDKAppEventNameAddedPaymentInfo</summary>
	public const string AddedPaymentInfo = "fb_mobile_add_payment_info";

	/// <summary>The user added an item to the cart. FBSDKAppEventNameAddedToCart</summary>
	public const string AddedToCart = "fb_mobile_add_to_cart";

	/// <summary>The user added an item to a wishlist. FBSDKAppEventNameAddedToWishlist</summary>
	public const string AddedToWishlist = "fb_mobile_add_to_wishlist";

	/// <summary>The user started a checkout. FBSDKAppEventNameInitiatedCheckout</summary>
	public const string InitiatedCheckout = "fb_mobile_initiated_checkout";

	/// <summary>
	/// Purchase. Prefer <see cref="FacebookAppEvents.LogPurchase"/>, which logs this name.
	/// FBSDKAppEventNamePurchased
	/// </summary>
	public const string Purchased = "fb_mobile_purchase";

	/// <summary>A purchase was restored (logged by automatic purchase logging). FBSDKAppEventNamePurchaseRestored</summary>
	public const string PurchaseRestored = "fb_mobile_purchase_restored";

	/// <summary>A subscription was restored (logged by automatic purchase logging). FBSDKAppEventNameSubscribeRestore</summary>
	public const string SubscribeRestore = "SubscriptionRestore";

	/// <summary>A purchase failed (logged by automatic purchase logging). FBSDKAppEventNamePurchaseFailed</summary>
	public const string PurchaseFailed = "fb_mobile_purchase_failed";

	/// <summary>A subscription checkout started (logged by automatic purchase logging). FBSDKAppEventNameSubscribeInitiatedCheckout</summary>
	public const string SubscribeInitiatedCheckout = "SubscriptionInitiatedCheckout";

	/// <summary>A subscription purchase failed (logged by automatic purchase logging). FBSDKAppEventNameSubscribeFailed</summary>
	public const string SubscribeFailed = "SubscriptionFailed";

	// Gaming.

	/// <summary>The user achieved a level. FBSDKAppEventNameAchievedLevel</summary>
	public const string AchievedLevel = "fb_mobile_level_achieved";

	/// <summary>The user unlocked an achievement. FBSDKAppEventNameUnlockedAchievement</summary>
	public const string UnlockedAchievement = "fb_mobile_achievement_unlocked";

	/// <summary>The user spent in-app credits. FBSDKAppEventNameSpentCredits</summary>
	public const string SpentCredits = "fb_mobile_spent_credits";

	// Logged by the SDK itself. Listed so Events Manager names can be matched; do not log them manually.

	/// <summary>
	/// App activation, logged by <see cref="FacebookAppEvents.ActivateApp"/> or by automatic logging.
	/// Internal on iOS, public on Android (<c>EVENT_NAME_ACTIVATED_APP</c>). FBSDKAppEventNameActivatedApp
	/// </summary>
	public const string ActivatedApp = "fb_mobile_activate_app";

	/// <summary>
	/// App deactivation, logged by the SDK. Internal on iOS, public on Android
	/// (<c>EVENT_NAME_DEACTIVATED_APP</c>). FBSDKAppEventNameDeactivatedApp
	/// </summary>
	public const string DeactivatedApp = "fb_mobile_deactivate_app";

	/// <summary>SDK initialization, logged by the SDK when automatic logging is on. FBSDKAppEventNameInitializeSDK</summary>
	public const string InitializeSdk = "fb_sdk_initialize";

	/// <summary>Background refresh available, logged by the SDK. FBSDKAppEventNameBackgroundStatusAvailable</summary>
	public const string BackgroundStatusAvailable = "fb_sdk_background_status_available";

	/// <summary>Background refresh denied, logged by the SDK. FBSDKAppEventNameBackgroundStatusDenied</summary>
	public const string BackgroundStatusDenied = "fb_sdk_background_status_denied";

	/// <summary>Background refresh restricted, logged by the SDK. FBSDKAppEventNameBackgroundStatusRestricted</summary>
	public const string BackgroundStatusRestricted = "fb_sdk_background_status_restricted";

	/// <summary>SDK settings changed, logged by the SDK. FBSDKAppEventNameSDKSettingsChanged</summary>
	public const string SdkSettingsChanged = "fb_sdk_settings_changed";
}
