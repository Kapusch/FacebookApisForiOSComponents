using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kapusch.Facebook.iOS;

public static unsafe partial class NativeFacebookShare
{
	public static void ConfigureAndInitialize(
		IntPtr uiApplicationHandle,
		bool trackingAllowed
	)
	{
		if (uiApplicationHandle == IntPtr.Zero)
			throw new ArgumentException("UIApplication is required.");

		ConfigureAndInitializeNative(
			uiApplicationHandle,
			trackingAllowed ? (byte)1 : (byte)0
		);
	}

	public static bool HandleOpenUrl(
		IntPtr uiApplicationHandle,
		IntPtr nsUrlHandle,
		IntPtr optionsHandle
	)
	{
		if (uiApplicationHandle == IntPtr.Zero || nsUrlHandle == IntPtr.Zero)
			return false;

		return HandleOpenUrlNative(
				uiApplicationHandle,
				nsUrlHandle,
				optionsHandle
			)
			!= 0;
	}

	public static Task<NativeFacebookShareResult> SharePhotoAsync(
		IntPtr presentingViewControllerHandle,
		string imagePath,
		CancellationToken cancellationToken = default
	)
	{
		if (presentingViewControllerHandle == IntPtr.Zero)
			throw new ArgumentException("Presenting view controller is required.");
		if (string.IsNullOrWhiteSpace(imagePath))
			throw new ArgumentException("Image path is required.", nameof(imagePath));
		if (cancellationToken.IsCancellationRequested)
			return Task.FromCanceled<NativeFacebookShareResult>(cancellationToken);

		var completion = new TaskCompletionSource<NativeFacebookShareResult>(
			TaskCreationOptions.RunContinuationsAsynchronously
		);
		var handle = GCHandle.Alloc(completion);
		var imagePathPointer = Marshal.StringToCoTaskMemUTF8(imagePath);
		try
		{
			SharePhotoNative(
				presentingViewControllerHandle,
				imagePathPointer,
				&ShareCallback,
				GCHandle.ToIntPtr(handle)
			);
		}
		catch
		{
			handle.Free();
			throw;
		}
		finally
		{
			Marshal.FreeCoTaskMem(imagePathPointer);
		}
		return completion.Task;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void ShareCallback(int status, IntPtr errorCode, IntPtr context)
	{
		var handle = GCHandle.FromIntPtr(context);
		var completion = (TaskCompletionSource<NativeFacebookShareResult>)handle.Target!;
		try
		{
			completion.TrySetResult(
				new NativeFacebookShareResult(
					(NativeFacebookShareStatus)status,
					Marshal.PtrToStringUTF8(errorCode)
				)
			);
		}
		finally
		{
			handle.Free();
		}
	}

	[LibraryImport(
		"__Internal",
		EntryPoint = "kfb_facebook_share_configure_and_initialize"
	)]
	private static partial void ConfigureAndInitializeNative(
		IntPtr uiApplicationHandle,
		byte trackingAllowed
	);

	[LibraryImport("__Internal", EntryPoint = "kfb_facebook_share_handle_open_url")]
	private static partial byte HandleOpenUrlNative(
		IntPtr uiApplicationHandle,
		IntPtr nsUrlHandle,
		IntPtr optionsHandle
	);

	[LibraryImport("__Internal", EntryPoint = "kfb_facebook_share_photo")]
	private static partial void SharePhotoNative(
		IntPtr presentingViewControllerHandle,
		IntPtr imagePath,
		delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void> callback,
		IntPtr context
	);
}
