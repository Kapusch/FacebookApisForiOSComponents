using System.Security.Cryptography;
using Foundation;
using Kapusch.Facebook.iOS;
using UIKit;

namespace Kapusch.Facebook.iOS.Sample;

[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
	public override UIWindow? Window { get; set; }

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
	{
		NativeFacebookLogin.Initialize(application.Handle, launchOptions?.Handle ?? IntPtr.Zero);

		Window = new UIWindow(UIScreen.MainScreen.Bounds);

		var viewController = new UIViewController();
		viewController.View!.BackgroundColor = UIColor.SystemBackground;

		var signInButton = new UIButton(UIButtonType.System);
		signInButton.SetTitle("Sign In (Limited)", UIControlState.Normal);
		signInButton.Frame = new CoreGraphics.CGRect(40, 120, 280, 44);
		signInButton.TouchUpInside += async (_, _) =>
		{
			var presenter = Window?.RootViewController;
			if (presenter is null)
				return;

			try
			{
				var rawNonce = GenerateRawNonce();
				var result = await NativeFacebookLogin.SignInAsync(
					presenter.Handle,
					FacebookTrackingMode.Limited,
					rawNonce: rawNonce,
					CancellationToken.None
				);

				Console.WriteLine(
					$"Facebook sign-in: Status={result.Status} UserId={result.UserId ?? "(null)"} Error={result.ErrorCode ?? "(null)"} {result.ErrorMessage ?? ""}"
				);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Facebook sign-in exception: {ex}");
			}
		};

		viewController.View.AddSubview(signInButton);

		Window.RootViewController = viewController;
		Window.MakeKeyAndVisible();

		return true;
	}

	private static string GenerateRawNonce(int size = 32)
	{
		var bytes = RandomNumberGenerator.GetBytes(size);
		return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}

	public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options) =>
		NativeFacebookLogin.HandleOpenUrl(
			application.Handle,
			url.Handle,
			options?.Handle ?? IntPtr.Zero
		);
}
