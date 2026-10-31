using System.Security.Cryptography;
using System.Text;
using Foundation;
using Kapusch.Facebook.iOS;
using UIKit;

namespace Kapusch.Facebook.iOS.Sample;

[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
	private bool _appEventsConsented;

	public override UIWindow? Window { get; set; }

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		// The SDK is not initialized at launch: it contacts Meta, so it waits for a login or for
		// App Events consent (Docs/Integration.md, section 4.2).
		Window = new UIWindow(UIScreen.MainScreen.Bounds);

		var viewController = new UIViewController();
		viewController.View!.BackgroundColor = UIColor.SystemBackground;

		var signInButton = new UIButton(UIButtonType.System);
		signInButton.SetTitle("Sign In (Limited)", UIControlState.Normal);
		signInButton.Frame = new CoreGraphics.CGRect(40, 120, 280, 44);
		signInButton.TouchUpInside += async (_, _) =>
		{
			var presenter = Window?.RootViewController;
			if (presenter is null || !HasFacebookConfiguration())
				return;

			try
			{
				EnsureSdkInitializedForLogin();

				// Firebase-style nonce: the SDK gets SHA-256(raw nonce) and puts it in the token's
				// nonce claim; the raw nonce stays in the app and goes to the backend with the token.
				var rawNonce = GenerateRawNonce();
				var hashedNonce = Sha256Hex(rawNonce);
				var result = await NativeFacebookLogin.SignInAsync(
					presenter.Handle,
					FacebookTrackingMode.Limited,
					hashedNonce,
					CancellationToken.None
				);

				Console.WriteLine(
					$"Facebook sign-in: Status={result.Status} "
						+ $"AuthenticationToken={(result.AuthenticationToken is null ? "none" : "present")} "
						+ $"NonceIsHashed={result.Nonce == hashedNonce} "
						+ $"Error={result.ErrorCode ?? "(null)"} {result.ErrorMessage ?? ""}"
				);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Facebook sign-in exception: {ex}");
			}
		};

		viewController.View.AddSubview(signInButton);
		AddAppEventsButtons(viewController.View);

		Window.RootViewController = viewController;
		Window.MakeKeyAndVisible();

		return true;
	}

	public override void OnActivated(UIApplication application)
	{
		// Automatic logging stays off (Info.plist), so log activations manually after consent.
		if (_appEventsConsented)
			FacebookAppEvents.ActivateApp();
	}

	private void AddAppEventsButtons(UIView view)
	{
		var y = 200;
		void AddButton(string title, Action action)
		{
			var button = new UIButton(UIButtonType.System);
			button.SetTitle(title, UIControlState.Normal);
			button.Frame = new CoreGraphics.CGRect(40, y, 280, 44);
			button.TouchUpInside += (_, _) =>
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"App Events exception: {ex}");
				}
			};
			view.AddSubview(button);
			y += 52;
		}

		AddButton("App Events: grant consent", GrantAppEventsConsent);
		AddButton(
			"App Events: log StartTrial",
			() =>
			{
				if (!EnsureConsented())
					return;

				FacebookAppEvents.LogStartTrial(
					value: 9.99m,
					currencyIsoCode: "EUR",
					orderId: "sample-subscription",
					parameters: new Dictionary<string, object>
					{
						[FacebookAppEventParameterNames.ContentId] = "sample.monthly",
					}
				);
				FacebookAppEvents.LogEvent("sample_custom_event", new Dictionary<string, object> { ["step"] = 1 });
				Console.WriteLine("App Events: logged StartTrial and sample_custom_event.");
			}
		);
		AddButton(
			"App Events: flush",
			() =>
			{
				if (EnsureConsented())
					FacebookAppEvents.Flush();
			}
		);
		AddButton("App Events: withdraw consent", WithdrawAppEventsConsent);
	}

	private void EnsureSdkInitializedForLogin()
	{
		if (FacebookAppEvents.IsSdkInitialized)
			return;

		if (!_appEventsConsented)
		{
			// Login before App Events consent: initialize with every switch off, so values a
			// previous session persisted do not apply.
			FacebookAppEvents.SetFlushBehavior(FacebookAppEventsFlushBehavior.ExplicitOnly);
			FacebookAppEvents.SetAutoLogAppEventsEnabled(false);
			FacebookAppEvents.SetAdvertiserIdCollectionEnabled(false);
			FacebookAppEvents.SetAdvertiserTrackingEnabled(false);
			FacebookAppEvents.SetSKAdNetworkReportEnabled(false);
		}

		FacebookAppEvents.InitializeSdk();
	}

	private void GrantAppEventsConsent()
	{
		if (!HasFacebookConfiguration())
			return;

		FacebookAppEvents.SetDebugLoggingEnabled(true);
		// A previous withdrawal in this process switched flushing to explicit only.
		FacebookAppEvents.SetFlushBehavior(FacebookAppEventsFlushBehavior.Auto);
		FacebookAppEvents.InitializeSdk();
		FacebookAppEvents.SetAdvertiserIdCollectionEnabled(true);
		FacebookAppEvents.SetAutoLogAppEventsEnabled(false);
		FacebookAppEvents.ActivateApp();
		_appEventsConsented = true;

		Console.WriteLine(
			$"App Events: SDK {FacebookAppEvents.SdkVersion}, anonymous ID {FacebookAppEvents.GetAnonymousId()}, "
				+ $"autoLog={FacebookAppEvents.IsAutoLogAppEventsEnabled}, "
				+ $"advertiserId={FacebookAppEvents.IsAdvertiserIdCollectionEnabled}, "
				+ $"initialized={FacebookAppEvents.IsSdkInitialized}"
		);
	}

	private void WithdrawAppEventsConsent()
	{
		_appEventsConsented = false;
		// Keep events queued before the withdrawal on the device (not persisted across launches).
		FacebookAppEvents.SetFlushBehavior(FacebookAppEventsFlushBehavior.ExplicitOnly);
		if (!FacebookAppEvents.IsSdkInitialized)
		{
			// Nothing is sent while the SDK is not initialized; the identity methods would throw.
			Console.WriteLine("App Events: consent withdrawn (SDK not initialized).");
			return;
		}

		FacebookAppEvents.SetAutoLogAppEventsEnabled(false);
		FacebookAppEvents.SetAdvertiserIdCollectionEnabled(false);
		FacebookAppEvents.ClearUserId();
		FacebookAppEvents.ClearUserData();
		Console.WriteLine("App Events: consent withdrawn.");
	}

	private bool EnsureConsented()
	{
		if (_appEventsConsented)
			return true;

		Console.WriteLine("App Events: grant consent first.");
		return false;
	}

	private static bool HasFacebookConfiguration()
	{
		// The SDK requires a Facebook app ID; the committed template has none.
		if (NSBundle.MainBundle.ObjectForInfoDictionary("FacebookAppID") is NSString)
			return true;

		Console.WriteLine("Facebook: add FacebookAppID and FacebookClientToken to the local Info.plist first.");
		return false;
	}

	private static string GenerateRawNonce(int size = 32)
	{
		var bytes = RandomNumberGenerator.GetBytes(size);
		return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}

	private static string Sha256Hex(string value) =>
		Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

	// Login forwards URLs to the SDK's ApplicationDelegate, which also serves App Events (AEM).
	public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options) =>
		NativeFacebookLogin.HandleOpenUrl(
			application.Handle,
			url.Handle,
			options?.Handle ?? IntPtr.Zero
		);

	public override bool ContinueUserActivity(
		UIApplication application,
		NSUserActivity userActivity,
		UIApplicationRestorationHandler completionHandler
	) => FacebookAppEvents.ContinueUserActivity(application.Handle, userActivity.Handle);
}
