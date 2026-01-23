using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kapusch.Facebook.iOS;

public static unsafe class NativeFacebookLogin
{
	private const string LibraryName = "__Internal";

	public static void Initialize(IntPtr uiApplicationHandle, IntPtr launchOptionsHandle)
	{
		if (uiApplicationHandle == IntPtr.Zero)
			return;

		try
		{
			KfiFacebookInitialize(
				uiApplicationHandle,
				launchOptionsHandle == IntPtr.Zero ? null : launchOptionsHandle
			);
		}
		catch
		{
			// Best-effort only.
		}
	}

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
			return KfiFacebookHandleOpenUrl(
				uiApplicationHandle,
				nsUrlHandle,
				optionsHandle == IntPtr.Zero ? null : optionsHandle
			);
		}
		catch
		{
			return false;
		}
	}

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

		var tcs = new TaskCompletionSource<NativeFacebookSignInResult>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);

		var gch = GCHandle.Alloc(tcs);
		var context = GCHandle.ToIntPtr(gch);

		KfiFacebookSignInStart(
			presentingViewControllerHandle,
			(int)trackingMode,
			rawNonce,
			&KfiFacebookCallback,
			context
		);

		_ = cancellationToken.Register(() =>
			tcs.TrySetResult(new NativeFacebookSignInResult(NativeSignInStatus.Cancelled))
		);

		return tcs.Task;
	}

	public static void SignOut()
	{
		try
		{
			KfiFacebookSignOut();
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
		var gch = GCHandle.FromIntPtr(context);
		var tcs = (TaskCompletionSource<NativeFacebookSignInResult>)gch.Target!;

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

			_ = tcs.TrySetResult(result);
		}
		finally
		{
			gch.Free();
		}
	}

	[DllImport(LibraryName, EntryPoint = "kfb_facebook_initialize")]
	private static extern void KfiFacebookInitialize(IntPtr uiApplication, IntPtr? launchOptions);

	[DllImport(LibraryName, EntryPoint = "kfb_facebook_handle_open_url")]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool KfiFacebookHandleOpenUrl(
		IntPtr uiApplication,
		IntPtr nsUrl,
		IntPtr? options
	);

	[DllImport(LibraryName, EntryPoint = "kfb_facebook_signin_start")]
	private static extern void KfiFacebookSignInStart(
		IntPtr presentingViewController,
		int trackingMode,
		[MarshalAs(UnmanagedType.LPUTF8Str)] string? rawNonce,
		delegate* unmanaged[Cdecl]<
			int,
			IntPtr,
			IntPtr,
			IntPtr,
			IntPtr,
			IntPtr,
			IntPtr,
			IntPtr,
			void> callback,
		IntPtr context
	);

	[DllImport(LibraryName, EntryPoint = "kfb_facebook_signout")]
	private static extern void KfiFacebookSignOut();
}
