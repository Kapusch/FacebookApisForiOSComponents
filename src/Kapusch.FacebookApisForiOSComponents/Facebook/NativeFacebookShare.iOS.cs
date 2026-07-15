using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kapusch.Facebook.iOS;

public static unsafe class NativeFacebookShare
{
	public static void ConfigureAndInitialize(
		IntPtr uiApplicationHandle,
		bool trackingAllowed
	)
	{
		if (uiApplicationHandle == IntPtr.Zero)
			throw new ArgumentException("UIApplication is required.");

		ResolveConfigureAndInitialize()(uiApplicationHandle, trackingAllowed ? (byte)1 : (byte)0);
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
			ResolveSharePhoto()(
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

	private static IntPtr Resolve(string symbol) =>
		NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), symbol);

	private static delegate* unmanaged[Cdecl]<IntPtr, byte, void> ResolveConfigureAndInitialize() =>
		(delegate* unmanaged[Cdecl]<IntPtr, byte, void>)Resolve(
			"kfb_facebook_share_configure_and_initialize"
		);

	private static delegate* unmanaged[Cdecl]<
		IntPtr,
		IntPtr,
		delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void>,
		IntPtr,
		void> ResolveSharePhoto() =>
		(delegate* unmanaged[Cdecl]<
			IntPtr,
			IntPtr,
			delegate* unmanaged[Cdecl]<int, IntPtr, IntPtr, void>,
			IntPtr,
			void>)Resolve("kfb_facebook_share_photo");
}
