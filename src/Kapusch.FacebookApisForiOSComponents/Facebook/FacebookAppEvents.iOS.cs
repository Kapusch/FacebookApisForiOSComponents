using System.Buffers;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Kapusch.Facebook.iOS;

/// <summary>
/// Meta App Events on iOS: SDK initialization, consent-related settings, event logging,
/// identity and AEM / deep-link forwarding. Thin facade over <c>FBSDKAppEvents</c>,
/// <c>FBSDKSettings</c> and <c>FBSDKApplicationDelegate</c> of the bundled Facebook iOS SDK.
/// </summary>
/// <remarks>
/// <para>
/// Available with every <c>KapuschFacebookFeatures</c> selection, because <c>FBSDKCoreKit</c>
/// is always linked. Select <c>AppEvents</c> alone to ship only CoreKit, AEMKit and
/// CoreKit_Basics.
/// </para>
/// <para>
/// The facade never enables anything on its own. Values set through the <c>Set…Enabled</c>
/// methods are persisted by the SDK and override the matching <c>Info.plist</c> keys on later
/// launches. Values set before <see cref="InitializeSdk"/> are applied again once the SDK is
/// initialized, so the SDK persists them as well.
/// </para>
/// <para>
/// iOS has no counterpart of Android's <c>setAutoInitEnabled</c>: the SDK only starts when the
/// app calls <see cref="InitializeSdk"/>, <see cref="FinishedLaunching"/>,
/// <c>NativeFacebookLogin.Initialize</c> or <c>NativeFacebookShare.ConfigureAndInitialize</c>.
/// </para>
/// <para>
/// Until one of them ran (<see cref="IsSdkInitialized"/>), the SDK silently drops events and
/// identity changes and reports default setting values. The event methods, the identity
/// methods and the setting getters therefore throw <see cref="InvalidOperationException"/>
/// before initialization, like the Android package; <see cref="HandleOpenUrl"/> and
/// <see cref="ContinueUserActivity"/> return <see langword="false"/> without forwarding.
/// The setters, <see cref="SetDataProcessingOptions"/>, <see cref="SetFlushBehavior"/>,
/// <see cref="SetDebugLoggingEnabled"/>, <see cref="GetAnonymousId"/> and
/// <see cref="SdkVersion"/> work at any time.
/// </para>
/// </remarks>
public static unsafe partial class FacebookAppEvents
{
	private const string NativeLibraryName = "__Internal";

	// SDK lifecycle.

	/// <summary>
	/// Gets whether the SDK was initialized in this process through this package
	/// (<see cref="InitializeSdk"/>, <see cref="FinishedLaunching"/>,
	/// <c>NativeFacebookLogin.Initialize</c> or <c>NativeFacebookShare.ConfigureAndInitialize</c>).
	/// </summary>
	public static bool IsSdkInitialized => IsSdkInitializedNative();

	/// <summary>
	/// Initializes the SDK (<c>application:didFinishLaunchingWithOptions:</c> with no launch
	/// options). Idempotent. Runs synchronously on the main thread; when called from another
	/// thread it is queued on the main thread and <see cref="IsSdkInitialized"/> becomes
	/// <see langword="true"/> once it ran.
	/// </summary>
	/// <remarks>
	/// Initialization contacts Meta to load the app's SDK configuration, even when automatic
	/// event logging is disabled. Call it only once the user has consented.
	/// </remarks>
	public static void InitializeSdk() => InitializeSdkNative();

	/// <summary>
	/// Forwards <c>UIApplicationDelegate.FinishedLaunching</c> to the SDK, including the launch
	/// options (a cold-start URL is used for Aggregated Event Measurement). This initializes the
	/// SDK like <see cref="InitializeSdk"/>; only call it at launch when the user has already
	/// consented. Must be called on the main thread.
	/// </summary>
	/// <param name="uiApplicationHandle"><c>UIApplication.Handle</c>.</param>
	/// <param name="launchOptionsHandle"><c>NSDictionary.Handle</c> of the launch options, or <see cref="IntPtr.Zero"/>.</param>
	/// <returns>The SDK result: <see langword="false"/> when the SDK was already initialized.</returns>
	public static bool FinishedLaunching(IntPtr uiApplicationHandle, IntPtr launchOptionsHandle)
	{
		if (uiApplicationHandle == IntPtr.Zero)
			throw new ArgumentException("UIApplication is required.", nameof(uiApplicationHandle));

		return FinishedLaunchingNative(uiApplicationHandle, launchOptionsHandle);
	}

	/// <summary>
	/// Forwards <c>UIApplicationDelegate.OpenUrl</c> (and scene URL contexts) to the SDK, for
	/// Aggregated Event Measurement and App Links. Must be called on the main thread. Before the
	/// SDK is initialized the URL is not forwarded, so nothing is recorded before consent.
	/// </summary>
	/// <returns><see langword="true"/> when the SDK handled the URL.</returns>
	public static bool HandleOpenUrl(IntPtr uiApplicationHandle, IntPtr nsUrlHandle, IntPtr optionsHandle)
	{
		if (uiApplicationHandle == IntPtr.Zero || nsUrlHandle == IntPtr.Zero)
			return false;

		return HandleOpenUrlNative(uiApplicationHandle, nsUrlHandle, optionsHandle);
	}

	/// <summary>
	/// Forwards <c>UIApplicationDelegate.ContinueUserActivity</c> (Universal Links) to the SDK,
	/// for Aggregated Event Measurement. Only browsing-web activities are used by the SDK. Must
	/// be called on the main thread. Before the SDK is initialized the activity is not forwarded.
	/// </summary>
	/// <returns><see langword="true"/> when the SDK handled the activity.</returns>
	public static bool ContinueUserActivity(IntPtr uiApplicationHandle, IntPtr nsUserActivityHandle)
	{
		if (uiApplicationHandle == IntPtr.Zero || nsUserActivityHandle == IntPtr.Zero)
			return false;

		return ContinueUserActivityNative(uiApplicationHandle, nsUserActivityHandle);
	}

	/// <summary>Version of the bundled Facebook iOS SDK, for diagnostics.</summary>
	public static string SdkVersion => TakeNativeString(CopySdkVersionNative()) ?? string.Empty;

	// Settings.

	/// <summary>
	/// Enables or disables automatic app event logging (<c>FacebookAutoLogAppEventsEnabled</c>):
	/// app activation, install attribution ping and, when enabled in the Meta dashboard,
	/// in-app purchases. When disabled, call <see cref="ActivateApp"/> to keep install and
	/// activation events.
	/// </summary>
	public static void SetAutoLogAppEventsEnabled(bool enabled) => SetAutoLogAppEventsEnabledNative(enabled);

	/// <summary>Current automatic app event logging setting.</summary>
	public static bool IsAutoLogAppEventsEnabled
	{
		get
		{
			// Checked first: before initialization the SDK reports defaults and caches some of them.
			EnsureSdkInitialized();
			return IsAutoLogAppEventsEnabledNative();
		}
	}

	/// <summary>
	/// Enables or disables advertiser ID (IDFA) collection
	/// (<c>FacebookAdvertiserIDCollectionEnabled</c>). iOS only returns a non-zero IDFA once App
	/// Tracking Transparency is authorized.
	/// </summary>
	public static void SetAdvertiserIdCollectionEnabled(bool enabled) => SetAdvertiserIdCollectionEnabledNative(enabled);

	/// <summary>Current advertiser ID collection setting.</summary>
	public static bool IsAdvertiserIdCollectionEnabled
	{
		get
		{
			// Checked first: before initialization the SDK reports defaults and caches some of them.
			EnsureSdkInitialized();
			return IsAdvertiserIdCollectionEnabledNative();
		}
	}

	/// <summary>
	/// Sets the advertiser tracking flag. Only used on iOS 15 and 16: on iOS 17 and later the SDK
	/// ignores it and reads the App Tracking Transparency status instead.
	/// </summary>
	public static void SetAdvertiserTrackingEnabled(bool enabled) => SetAdvertiserTrackingEnabledNative(enabled);

	/// <summary>Current advertiser tracking flag (derived from App Tracking Transparency on iOS 17+).</summary>
	public static bool IsAdvertiserTrackingEnabled
	{
		get
		{
			// Checked first: before initialization the SDK reports defaults and caches some of them.
			EnsureSdkInitialized();
			return IsAdvertiserTrackingEnabledNative();
		}
	}

	/// <summary>
	/// Enables or disables SKAdNetwork reporting by the SDK (<c>FacebookSKAdNetworkReportEnabled</c>).
	/// Disable it when another SDK owns SKAdNetwork conversion values.
	/// </summary>
	public static void SetSKAdNetworkReportEnabled(bool enabled) => SetSKAdNetworkReportEnabledNative(enabled);

	/// <summary>Current SKAdNetwork reporting setting.</summary>
	public static bool IsSKAdNetworkReportEnabled
	{
		get
		{
			// Checked first: before initialization the SDK reports defaults and caches some of them.
			EnsureSdkInitialized();
			return IsSKAdNetworkReportEnabledNative();
		}
	}

	/// <summary>
	/// Sets data processing options, for example
	/// <c>[FacebookDataProcessingOptions.LimitedDataUse]</c> with country and state 0 to let Meta
	/// geolocate the user. An empty array explicitly disables Limited Data Use.
	/// </summary>
	public static void SetDataProcessingOptions(string[] options, int country = 0, int state = 0)
	{
		ArgumentNullException.ThrowIfNull(options);
		foreach (var option in options)
		{
			if (string.IsNullOrWhiteSpace(option))
				throw new ArgumentException("Data processing options cannot be empty.", nameof(options));
		}

		var buffer = new ArrayBufferWriter<byte>();
		using (var writer = new Utf8JsonWriter(buffer))
		{
			writer.WriteStartArray();
			foreach (var option in options)
				writer.WriteStringValue(option);
			writer.WriteEndArray();
		}

		fixed (byte* json = NullTerminated(buffer))
			SetDataProcessingOptionsNative(json, country, state);
	}

	/// <summary>
	/// Enables or disables SDK console logging for app events, developer errors and
	/// informational messages, for diagnostics (shown in the device log / Xcode console).
	/// Disabling restores the logging behaviours that were active before.
	/// </summary>
	public static void SetDebugLoggingEnabled(bool enabled) => SetDebugLoggingEnabledNative(enabled);

	// Event logging.

	/// <summary>
	/// Logs <c>fb_mobile_activate_app</c> and, once per install, the install attribution ping.
	/// Call it when the app becomes active if automatic logging is disabled. The SDK only logs
	/// an activation after 60 seconds or more of inactivity.
	/// </summary>
	public static void ActivateApp()
	{
		EnsureSdkInitialized();
		ActivateAppNative();
	}

	/// <summary>Logs an app event.</summary>
	/// <param name="eventName">A <see cref="FacebookAppEventNames"/> constant or a custom name (2-40 characters: letters, digits, <c>_</c>, <c>-</c> or spaces).</param>
	/// <param name="parameters">Optional parameters. Values must be strings, numbers or booleans (sent as <c>1</c>/<c>0</c>).</param>
	/// <param name="valueToSum">Optional value summed across events of this name, for example a price.</param>
	public static void LogEvent(
		string eventName,
		IReadOnlyDictionary<string, object>? parameters = null,
		double? valueToSum = null
	)
	{
		if (string.IsNullOrWhiteSpace(eventName))
			throw new ArgumentException("Event name is required.", nameof(eventName));
		if (valueToSum is { } value && !double.IsFinite(value))
			throw new ArgumentOutOfRangeException(nameof(valueToSum), "Value to sum must be finite.");

		var json = SerializeParameters(parameters);
		EnsureSdkInitialized();
		bool logged;
		fixed (byte* parametersJson = json)
		{
			logged = LogEventNative(
				eventName,
				parametersJson,
				valueToSum.HasValue,
				valueToSum.GetValueOrDefault()
			);
		}

		if (!logged)
			throw new InvalidOperationException("The native App Events facade rejected the event.");
	}

	/// <summary>
	/// Logs <c>fb_mobile_purchase</c>. The SDK rounds the amount to three decimals and flushes
	/// immediately unless the flush behaviour is <see cref="FacebookAppEventsFlushBehavior.ExplicitOnly"/>.
	/// Do not use it for purchases that a server already reports to Meta.
	/// </summary>
	/// <param name="amount">Purchase amount in <paramref name="currencyIsoCode"/>.</param>
	/// <param name="currencyIsoCode">ISO 4217 currency code, for example <c>EUR</c>.</param>
	/// <param name="parameters">Optional parameters (strings, numbers or booleans).</param>
	public static void LogPurchase(
		decimal amount,
		string currencyIsoCode,
		IReadOnlyDictionary<string, object>? parameters = null
	)
	{
		var currency = NormalizeCurrency(currencyIsoCode, nameof(currencyIsoCode));
		var json = SerializeParameters(parameters);
		EnsureSdkInitialized();
		bool logged;
		fixed (byte* parametersJson = json)
			logged = LogPurchaseNative(decimal.ToDouble(amount), currency, parametersJson);

		if (!logged)
			throw new InvalidOperationException("The native App Events facade rejected the purchase.");
	}

	/// <summary>
	/// Logs <see cref="FacebookAppEventNames.StartTrial"/>. Do not use it when a server (for
	/// example a subscription platform) already sends trial events to Meta.
	/// </summary>
	/// <param name="value">Optional value (for example the price after the trial), sent as <c>valueToSum</c>.</param>
	/// <param name="currencyIsoCode">ISO 4217 currency code; required with <paramref name="value"/>.</param>
	/// <param name="orderId">Optional subscription identifier, sent as <c>fb_order_id</c>.</param>
	/// <param name="parameters">Optional extra parameters. The typed arguments win on conflicts.</param>
	public static void LogStartTrial(
		decimal? value = null,
		string? currencyIsoCode = null,
		string? orderId = null,
		IReadOnlyDictionary<string, object>? parameters = null
	) => LogValueEvent(FacebookAppEventNames.StartTrial, value, currencyIsoCode, orderId, parameters);

	/// <summary>
	/// Logs <see cref="FacebookAppEventNames.Subscribe"/>. Do not use it when a server (for
	/// example a subscription platform) already sends subscription events to Meta.
	/// </summary>
	/// <param name="value">Optional subscription price, sent as <c>valueToSum</c>.</param>
	/// <param name="currencyIsoCode">ISO 4217 currency code; required with <paramref name="value"/>.</param>
	/// <param name="orderId">Optional subscription identifier, sent as <c>fb_order_id</c>.</param>
	/// <param name="parameters">Optional extra parameters. The typed arguments win on conflicts.</param>
	public static void LogSubscribe(
		decimal? value = null,
		string? currencyIsoCode = null,
		string? orderId = null,
		IReadOnlyDictionary<string, object>? parameters = null
	) => LogValueEvent(FacebookAppEventNames.Subscribe, value, currencyIsoCode, orderId, parameters);

	/// <summary>Logs <see cref="FacebookAppEventNames.CompletedRegistration"/>.</summary>
	/// <param name="registrationMethod">Optional method, for example <c>email</c>, sent as <c>fb_registration_method</c>.</param>
	/// <param name="value">Optional value, sent as <c>valueToSum</c>.</param>
	/// <param name="currencyIsoCode">ISO 4217 currency code; required with <paramref name="value"/>.</param>
	/// <param name="parameters">Optional extra parameters. The typed arguments win on conflicts.</param>
	public static void LogCompleteRegistration(
		string? registrationMethod = null,
		decimal? value = null,
		string? currencyIsoCode = null,
		IReadOnlyDictionary<string, object>? parameters = null
	)
	{
		var merged = MergeParameters(parameters);
		if (!string.IsNullOrWhiteSpace(registrationMethod))
			merged[FacebookAppEventParameterNames.RegistrationMethod] = registrationMethod;

		LogValueEvent(FacebookAppEventNames.CompletedRegistration, value, currencyIsoCode, orderId: null, merged);
	}

	/// <summary>Sends queued events now.</summary>
	public static void Flush()
	{
		EnsureSdkInitialized();
		FlushNative();
	}

	/// <summary>Sets when queued events are sent.</summary>
	public static void SetFlushBehavior(FacebookAppEventsFlushBehavior behavior)
	{
		if (!Enum.IsDefined(behavior))
			throw new ArgumentOutOfRangeException(nameof(behavior));

		SetFlushBehaviorNative((int)behavior);
	}

	/// <summary>Current flush behaviour.</summary>
	public static FacebookAppEventsFlushBehavior FlushBehavior =>
		(FacebookAppEventsFlushBehavior)GetFlushBehaviorNative();

	// Identity and advanced matching.

	/// <summary>
	/// Anonymous, SDK-generated install identifier (<c>AppEvents.shared.anonymousID</c>), for
	/// example for server-side attribution partners. Generated and persisted on first access.
	/// </summary>
	public static string? GetAnonymousId() => TakeNativeString(CopyAnonymousIdNative());

	/// <summary>
	/// Associates a custom user ID with subsequent events. Persisted by the SDK until cleared.
	/// <see langword="null"/> or empty clears it.
	/// </summary>
	public static void SetUserId(string? userId)
	{
		EnsureSdkInitialized();
		SetUserIdNative(string.IsNullOrEmpty(userId) ? null : userId);
	}

	/// <summary>Clears the custom user ID.</summary>
	public static void ClearUserId() => SetUserId(null);

	/// <summary>Current custom user ID, if any.</summary>
	public static string? GetUserId()
	{
		EnsureSdkInitialized();
		return TakeNativeString(CopyUserIdNative());
	}

	/// <summary>
	/// Sets advanced-matching user data. Replaces the ten standard fields at once; a
	/// <see langword="null"/> property clears that field. The SDK hashes the values.
	/// </summary>
	public static void SetUserData(FacebookAppEventsUserData userData)
	{
		ArgumentNullException.ThrowIfNull(userData);
		EnsureSdkInitialized();
		SetUserDataNative(
			userData.Email,
			userData.FirstName,
			userData.LastName,
			userData.Phone,
			userData.DateOfBirth,
			userData.Gender,
			userData.City,
			userData.State,
			userData.Zip,
			userData.Country
		);
	}

	/// <summary>
	/// Sets one advanced-matching value, for example
	/// <see cref="FacebookAppEventUserDataTypes.ExternalId"/>. <see langword="null"/> clears it.
	/// </summary>
	public static void SetUserDataValue(string userDataType, string? value)
	{
		if (string.IsNullOrWhiteSpace(userDataType))
			throw new ArgumentException("User data type is required.", nameof(userDataType));

		EnsureSdkInitialized();
		SetUserDataForTypeNative(userDataType, value);
	}

	/// <summary>Clears all advanced-matching user data.</summary>
	public static void ClearUserData()
	{
		EnsureSdkInitialized();
		ClearUserDataNative();
	}

	/// <summary>Clears one advanced-matching value.</summary>
	public static void ClearUserDataValue(string userDataType)
	{
		if (string.IsNullOrWhiteSpace(userDataType))
			throw new ArgumentException("User data type is required.", nameof(userDataType));

		EnsureSdkInitialized();
		ClearUserDataForTypeNative(userDataType);
	}

	// Helpers.

	private static void EnsureSdkInitialized()
	{
		if (!IsSdkInitializedNative())
		{
			throw new InvalidOperationException(
				"The Facebook SDK is not initialized: call FacebookAppEvents.InitializeSdk() (or FinishedLaunching, "
					+ "NativeFacebookLogin.Initialize, NativeFacebookShare.ConfigureAndInitialize) first. "
					+ "Before initialization the SDK would drop this call silently."
			);
		}
	}

	private static void LogValueEvent(
		string eventName,
		decimal? value,
		string? currencyIsoCode,
		string? orderId,
		IReadOnlyDictionary<string, object>? parameters
	)
	{
		var merged = MergeParameters(parameters);
		if (value.HasValue && string.IsNullOrWhiteSpace(currencyIsoCode))
			throw new ArgumentException("A currency is required with a value.", nameof(currencyIsoCode));
		if (!string.IsNullOrWhiteSpace(currencyIsoCode))
			merged[FacebookAppEventParameterNames.Currency] = NormalizeCurrency(currencyIsoCode, nameof(currencyIsoCode));
		if (!string.IsNullOrWhiteSpace(orderId))
			merged[FacebookAppEventParameterNames.OrderId] = orderId;

		LogEvent(eventName, merged, value.HasValue ? decimal.ToDouble(value.Value) : null);
	}

	private static Dictionary<string, object> MergeParameters(IReadOnlyDictionary<string, object>? parameters)
	{
		var merged = new Dictionary<string, object>(StringComparer.Ordinal);
		if (parameters is null)
			return merged;

		foreach (var (key, value) in parameters)
			merged[key] = value;
		return merged;
	}

	private static string NormalizeCurrency(string? currencyIsoCode, string parameterName)
	{
		var currency = currencyIsoCode?.Trim();
		if (currency is not { Length: 3 } || !currency.All(char.IsAsciiLetter))
			throw new ArgumentException("Currency must be an ISO 4217 code such as EUR.", parameterName);

		return currency.ToUpperInvariant();
	}

	private static byte[]? SerializeParameters(IReadOnlyDictionary<string, object>? parameters)
	{
		if (parameters is null || parameters.Count == 0)
			return null;

		var buffer = new ArrayBufferWriter<byte>();
		using (var writer = new Utf8JsonWriter(buffer))
		{
			writer.WriteStartObject();
			foreach (var (key, value) in parameters)
			{
				if (string.IsNullOrWhiteSpace(key))
					throw new ArgumentException("Parameter names cannot be empty.", nameof(parameters));

				writer.WritePropertyName(key);
				WriteParameterValue(writer, key, value);
			}
			writer.WriteEndObject();
		}

		return NullTerminated(buffer);
	}

	private static void WriteParameterValue(Utf8JsonWriter writer, string key, object? value)
	{
		switch (value)
		{
			case string text:
				writer.WriteStringValue(text);
				break;
			case bool flag:
				writer.WriteStringValue(flag ? FacebookAppEventParameterValues.Yes : FacebookAppEventParameterValues.No);
				break;
			case byte or sbyte or short or ushort or int or uint or long:
				writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
				break;
			case ulong unsignedLong:
				writer.WriteNumberValue(unsignedLong);
				break;
			case float or double:
				var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
				if (!double.IsFinite(number))
					throw new ArgumentException($"Parameter '{key}' must be a finite number.", nameof(value));
				writer.WriteNumberValue(number);
				break;
			case decimal amount:
				writer.WriteNumberValue(amount);
				break;
			default:
				throw new ArgumentException(
					$"Parameter '{key}' must be a string, a number or a boolean.",
					nameof(value)
				);
		}
	}

	private static byte[] NullTerminated(ArrayBufferWriter<byte> buffer)
	{
		var bytes = new byte[buffer.WrittenCount + 1];
		buffer.WrittenSpan.CopyTo(bytes);
		return bytes;
	}

	private static string? TakeNativeString(IntPtr pointer)
	{
		if (pointer == IntPtr.Zero)
			return null;

		try
		{
			return Marshal.PtrToStringUTF8(pointer);
		}
		finally
		{
			FreeNative(pointer);
		}
	}

	// Native entry points (kfbappevents.xcframework, always linked).

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_initialize_sdk")]
	private static partial void InitializeSdkNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_finished_launching")]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool FinishedLaunchingNative(IntPtr uiApplicationHandle, IntPtr launchOptionsHandle);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_handle_open_url")]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool HandleOpenUrlNative(IntPtr uiApplicationHandle, IntPtr nsUrlHandle, IntPtr optionsHandle);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_continue_user_activity")]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool ContinueUserActivityNative(IntPtr uiApplicationHandle, IntPtr nsUserActivityHandle);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_is_sdk_initialized")]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool IsSdkInitializedNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_copy_sdk_version")]
	private static partial IntPtr CopySdkVersionNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_auto_log_app_events_enabled")]
	private static partial void SetAutoLogAppEventsEnabledNative([MarshalAs(UnmanagedType.U1)] bool enabled);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_is_auto_log_app_events_enabled")]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool IsAutoLogAppEventsEnabledNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_advertiser_id_collection_enabled")]
	private static partial void SetAdvertiserIdCollectionEnabledNative([MarshalAs(UnmanagedType.U1)] bool enabled);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_is_advertiser_id_collection_enabled")]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool IsAdvertiserIdCollectionEnabledNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_advertiser_tracking_enabled")]
	private static partial void SetAdvertiserTrackingEnabledNative([MarshalAs(UnmanagedType.U1)] bool enabled);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_is_advertiser_tracking_enabled")]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool IsAdvertiserTrackingEnabledNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_skadnetwork_report_enabled")]
	private static partial void SetSKAdNetworkReportEnabledNative([MarshalAs(UnmanagedType.U1)] bool enabled);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_is_skadnetwork_report_enabled")]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool IsSKAdNetworkReportEnabledNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_data_processing_options")]
	private static partial void SetDataProcessingOptionsNative(byte* optionsJson, int country, int state);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_debug_logging_enabled")]
	private static partial void SetDebugLoggingEnabledNative([MarshalAs(UnmanagedType.U1)] bool enabled);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_activate_app")]
	private static partial void ActivateAppNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_log_event", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool LogEventNative(
		string eventName,
		byte* parametersJson,
		[MarshalAs(UnmanagedType.U1)] bool hasValueToSum,
		double valueToSum
	);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_log_purchase", StringMarshalling = StringMarshalling.Utf8)]
	[return: MarshalAs(UnmanagedType.U1)]
	private static partial bool LogPurchaseNative(double amount, string currency, byte* parametersJson);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_flush")]
	private static partial void FlushNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_flush_behavior")]
	private static partial void SetFlushBehaviorNative(int behavior);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_get_flush_behavior")]
	private static partial int GetFlushBehaviorNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_copy_anonymous_id")]
	private static partial IntPtr CopyAnonymousIdNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_user_id", StringMarshalling = StringMarshalling.Utf8)]
	private static partial void SetUserIdNative(string? userId);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_copy_user_id")]
	private static partial IntPtr CopyUserIdNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_user_data", StringMarshalling = StringMarshalling.Utf8)]
	private static partial void SetUserDataNative(
		string? email,
		string? firstName,
		string? lastName,
		string? phone,
		string? dateOfBirth,
		string? gender,
		string? city,
		string? state,
		string? zip,
		string? country
	);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_set_user_data_for_type", StringMarshalling = StringMarshalling.Utf8)]
	private static partial void SetUserDataForTypeNative(string type, string? value);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_clear_user_data")]
	private static partial void ClearUserDataNative();

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_clear_user_data_for_type", StringMarshalling = StringMarshalling.Utf8)]
	private static partial void ClearUserDataForTypeNative(string type);

	[LibraryImport(NativeLibraryName, EntryPoint = "kfb_appevents_free")]
	private static partial void FreeNative(IntPtr pointer);
}
