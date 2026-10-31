using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kapusch.Facebook.iOS;

/// <summary>
/// Facebook Login (LoginKit of the bundled Facebook iOS SDK 18.0.2). Requires
/// <c>KapuschFacebookFeatures</c> with <c>Login</c>; otherwise <see cref="SignInAsync"/>
/// completes with <see cref="NativeSignInStatus.Failed"/> / <c>login_feature_not_selected</c>.
/// </summary>
/// <remarks>
/// The SDK must be initialized before <see cref="SignInAsync"/> and <see cref="SignOut"/>,
/// preferably through <see cref="FacebookAppEvents.InitializeSdk"/> once the app's switches are
/// set (see <c>Docs/Integration.md</c>). Every method except <see cref="HandleOpenUrl"/> may be
/// called from any thread: the native wrapper runs LoginKit on the main thread.
/// </remarks>
public static unsafe partial class NativeFacebookLogin
{
	/// <summary>
	/// Initializes the Facebook SDK. Same initialization as
	/// <see cref="FacebookAppEvents.FinishedLaunching"/>, which it runs natively: the settings set
	/// through <see cref="FacebookAppEvents"/> before it are applied again, and
	/// <see cref="FacebookAppEvents.IsSdkInitialized"/> becomes <see langword="true"/>. Prefer
	/// calling <see cref="FacebookAppEvents"/> directly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Initialization contacts Meta and, for every switch the app did not set (through
	/// <see cref="FacebookAppEvents"/> or <c>Info.plist</c>), uses the SDK defaults: automatic
	/// app event logging and advertiser ID collection on. It changes no setting on its own. In
	/// an app that gates App Events on consent, set the switches first and never call it
	/// unconditionally at launch.
	/// </para>
	/// <para>
	/// Runs synchronously on the main thread; from another thread it is queued on the main
	/// thread. Best-effort: never throws.
	/// </para>
	/// </remarks>
	/// <param name="uiApplicationHandle"><c>UIApplication.Handle</c>.</param>
	/// <param name="launchOptionsHandle"><c>NSDictionary.Handle</c> of the launch options, or <see cref="IntPtr.Zero"/>.</param>
	public static void Initialize(IntPtr uiApplicationHandle, IntPtr launchOptionsHandle)
	{
		if (uiApplicationHandle == IntPtr.Zero)
			return;

		try
		{
			InitializeNative(uiApplicationHandle, launchOptionsHandle);
		}
		catch
		{
			// Best-effort only.
		}
	}

	/// <summary>
	/// Forwards a URL opened by the app to the SDK (the <c>fb&lt;App ID&gt;</c> login callback).
	/// Call it on the main thread, as <c>UIApplicationDelegate.OpenUrl</c> does.
	/// </summary>
	/// <returns><see langword="true"/> when the SDK handled the URL.</returns>
	public static bool HandleOpenUrl(
		IntPtr uiApplicationHandle,
		IntPtr nsUrlHandle,
		IntPtr optionsHandle
	)
	{
		if (uiApplicationHandle == IntPtr.Zero || nsUrlHandle == IntPtr.Zero)
			return false;

		try
		{
			return HandleOpenUrlNative(uiApplicationHandle, nsUrlHandle, optionsHandle) != 0;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Starts a Facebook login with the <c>public_profile</c> and <c>email</c> permissions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Nonce.</b> Despite its historical name, <paramref name="rawNonce"/> is the value the SDK
	/// puts in the <c>nonce</c> claim of the returned authentication token, and the value returned
	/// in <see cref="NativeFacebookSignInResult.Nonce"/>. For Firebase Authentication, generate a
	/// random raw nonce, keep it, pass its SHA-256 here (lowercase hexadecimal) and give Firebase
	/// the authentication token with the raw nonce: Firebase hashes the raw nonce and compares it
	/// with the claim. Passing the raw nonce here makes Firebase reject the token.
	/// </para>
	/// <para>
	/// <b>Result.</b> A <see cref="NativeSignInStatus.Success"/> carries at least one token, both
	/// from this login only: <see cref="NativeFacebookSignInResult.AccessToken"/> (classic
	/// login) and/or <see cref="NativeFacebookSignInResult.AuthenticationToken"/> (OIDC, Limited
	/// Login). A login that returned neither fails with <c>missing_token</c>; an authentication
	/// token whose nonce claim differs from the requested nonce fails with <c>nonce_mismatch</c>.
	/// </para>
	/// <para>
	/// <b>Concurrency.</b> One login at a time: a call while another is pending completes with
	/// <see cref="NativeSignInStatus.Failed"/> / <c>already_in_progress</c>. May be called from any
	/// thread; the login runs on the main thread. The SDK must be initialized first.
	/// </para>
	/// <para>
	/// <b>Cancellation.</b> Cancelling <paramref name="cancellationToken"/> completes the task with
	/// <see cref="NativeSignInStatus.Cancelled"/> once the wrapper released the pending login, so a
	/// new call can start as soon as the task completed. LoginKit 18.0.2 cannot dismiss its login
	/// UI: it stays up until the user closes it or the next login replaces it. Its outcome is
	/// discarded and, if it was a successful login, the SDK tokens it stored are cleared. While
	/// that abandoned UI is still up, LoginKit reports the next login as
	/// <see cref="NativeSignInStatus.Cancelled"/> almost immediately, without UI, as it tears the
	/// abandoned session down; the login after it works normally.
	/// </para>
	/// </remarks>
	/// <param name="presentingViewControllerHandle"><c>UIViewController.Handle</c> that presents the login UI.</param>
	/// <param name="trackingMode"><see cref="FacebookTrackingMode.Limited"/> requires a nonce.</param>
	/// <param name="rawNonce">
	/// Nonce for the authentication token's <c>nonce</c> claim; for Firebase, the SHA-256 of the
	/// raw nonce the app keeps (see remarks). Leading and trailing white space is ignored.
	/// Required with <see cref="FacebookTrackingMode.Limited"/> (<c>missing_nonce</c> otherwise);
	/// with <see cref="FacebookTrackingMode.Enabled"/>, <see langword="null"/> lets the SDK pick one.
	/// </param>
	/// <param name="cancellationToken">Abandons the pending login (see remarks).</param>
	/// <exception cref="ArgumentException"><paramref name="presentingViewControllerHandle"/> is <see cref="IntPtr.Zero"/>.</exception>
	public static Task<NativeFacebookSignInResult> SignInAsync(
		IntPtr presentingViewControllerHandle,
		FacebookTrackingMode trackingMode,
		string? rawNonce,
		CancellationToken cancellationToken = default
	)
	{
		if (presentingViewControllerHandle == IntPtr.Zero)
			throw new ArgumentException("Presenting view controller is required.");

		if (cancellationToken.IsCancellationRequested)
			return Task.FromResult(new NativeFacebookSignInResult(NativeSignInStatus.Cancelled));

		// The context is a process-unique request id, never a reusable handle: a late callback or a late
		// cancellation can never reach another request.
		var requestId = NativeFacebookSignInRequests.Begin(out var task);

		var noncePointer = string.IsNullOrEmpty(rawNonce)
			? IntPtr.Zero
			: Marshal.StringToCoTaskMemUTF8(rawNonce);
		try
		{
			SignInStartNative(
				presentingViewControllerHandle,
				(int)trackingMode,
				noncePointer,
				&KfiFacebookCallback,
				(IntPtr)requestId
			);
		}
		catch
		{
			NativeFacebookSignInRequests.Abandon(requestId);
			throw;
		}
		finally
		{
			if (noncePointer != IntPtr.Zero)
				Marshal.FreeCoTaskMem(noncePointer);
		}

		NativeFacebookSignInRequests.AttachCancellation(
			requestId,
			cancellationToken,
			static id => SignInCancelNative((IntPtr)id)
		);

		return task;
	}

	/// <summary>
	/// Clears the SDK's current access token, authentication token and profile (a local
	/// logout; the Facebook account stays signed in). The SDK must be initialized, as its token
	/// cache is only set up then. May be called from any thread.
	/// </summary>
	public static void SignOut()
	{
		try
		{
			SignOutNative();
		}
		catch
		{
			// Best-effort only.
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void KfiFacebookCallback(
		int status,
		IntPtr accessToken,
		IntPtr authenticationToken,
		IntPtr userId,
		IntPtr nonce,
		IntPtr errorCode,
		IntPtr errorMessage,
		IntPtr context
	)
	{
		try
		{
			var result = new NativeFacebookSignInResult(
				Status: (NativeSignInStatus)status,
				AccessToken: Marshal.PtrToStringUTF8(accessToken),
				AuthenticationToken: Marshal.PtrToStringUTF8(authenticationToken),
				UserId: Marshal.PtrToStringUTF8(userId),
				Nonce: Marshal.PtrToStringUTF8(nonce),
				ErrorCode: Marshal.PtrToStringUTF8(errorCode),
				ErrorMessage: Marshal.PtrToStringUTF8(errorMessage)
			);

			// Ignored for an unknown or already completed request.
			_ = NativeFacebookSignInRequests.Complete((long)context, result);
		}
		catch
		{
			// Never let an exception cross the native boundary.
		}
	}

	// Static __Internal imports make the .NET iOS build ask the native linker for
	// these symbols, so they survive dead stripping. The previous run-time lookup in
	// the main program found nothing once the linker had stripped them.

	[LibraryImport("__Internal", EntryPoint = "kfb_facebook_initialize")]
	private static partial void InitializeNative(IntPtr uiApplicationHandle, IntPtr launchOptionsHandle);

	[LibraryImport("__Internal", EntryPoint = "kfb_facebook_handle_open_url")]
	private static partial byte HandleOpenUrlNative(
		IntPtr uiApplicationHandle,
		IntPtr nsUrlHandle,
		IntPtr optionsHandle
	);

	[LibraryImport("__Internal", EntryPoint = "kfb_facebook_signin_start")]
	private static partial void SignInStartNative(
		IntPtr presentingViewControllerHandle,
		int trackingMode,
		IntPtr nonce,
		delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void> callback,
		IntPtr context
	);

	[LibraryImport("__Internal", EntryPoint = "kfb_facebook_signin_cancel")]
	private static partial void SignInCancelNative(IntPtr context);

	[LibraryImport("__Internal", EntryPoint = "kfb_facebook_signout")]
	private static partial void SignOutNative();
}
