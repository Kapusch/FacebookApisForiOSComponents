using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kapusch.Facebook.iOS;

public static unsafe class NativeFacebookLogin
{
	public static void Initialize(IntPtr uiApplicationHandle, IntPtr launchOptionsHandle)
	{
		if (uiApplicationHandle == IntPtr.Zero)
			return;

		try
		{
			ResolveInitialize()(
				uiApplicationHandle,
				launchOptionsHandle
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
			return ResolveHandleOpenUrl()(
				uiApplicationHandle,
				nsUrlHandle,
				optionsHandle
			) != 0;
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

		var noncePointer = string.IsNullOrEmpty(rawNonce)
			? IntPtr.Zero
			: Marshal.StringToCoTaskMemUTF8(rawNonce);
		try
		{
			ResolveSignInStart()(
				presentingViewControllerHandle,
				(int)trackingMode,
				noncePointer,
				&KfiFacebookCallback,
				context
			);
		}
		catch
		{
			gch.Free();
			throw;
		}
		finally
		{
			if (noncePointer != IntPtr.Zero)
				Marshal.FreeCoTaskMem(noncePointer);
		}

		_ = cancellationToken.Register(() =>
			tcs.TrySetResult(new NativeFacebookSignInResult(NativeSignInStatus.Cancelled))
		);

		return tcs.Task;
	}

	public static void SignOut()
	{
		try
		{
			ResolveSignOut()();
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

	private static IntPtr Resolve(string symbol) =>
		NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), symbol);

	private static delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> ResolveInitialize() =>
		(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void>)Resolve("kfb_facebook_initialize");

	private static delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, byte> ResolveHandleOpenUrl() =>
		(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, byte>)Resolve(
			"kfb_facebook_handle_open_url"
		);

	private static delegate* unmanaged[Cdecl]<
		IntPtr,
		int,
		IntPtr,
		delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>,
		IntPtr,
		void> ResolveSignInStart() =>
		(delegate* unmanaged[Cdecl]<
			IntPtr,
			int,
			IntPtr,
			delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>,
			IntPtr,
			void>)Resolve("kfb_facebook_signin_start");

	private static delegate* unmanaged[Cdecl]<void> ResolveSignOut() =>
		(delegate* unmanaged[Cdecl]<void>)Resolve("kfb_facebook_signout");
}
