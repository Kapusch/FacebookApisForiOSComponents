using System.Collections.Concurrent;

namespace Kapusch.Facebook.iOS;

/// <summary>
/// Pending <see cref="NativeFacebookLogin.SignInAsync"/> requests. Each request has a
/// process-unique id, passed to the native wrapper as its callback context, so a late native
/// callback or a late cancellation can never reach another request (a freed <c>GCHandle</c>
/// value can be reused). Platform-neutral so it can be unit tested without iOS.
/// </summary>
internal static class NativeFacebookSignInRequests
{
	private static readonly ConcurrentDictionary<long, Request> Pending = new();
	private static long _lastId;

	/// <summary>Number of requests waiting for their native callback (tests and diagnostics).</summary>
	internal static int PendingCount => Pending.Count;

	/// <summary>Registers a new request and returns its id; <paramref name="task"/> completes with its result.</summary>
	public static long Begin(out Task<NativeFacebookSignInResult> task)
	{
		var id = Interlocked.Increment(ref _lastId);
		var request = new Request();
		Pending[id] = request;
		task = request.Completion.Task;
		return id;
	}

	/// <summary>Forgets a request whose native start failed; its task never completes.</summary>
	public static void Abandon(long id)
	{
		if (Pending.TryRemove(id, out var request))
			request.ReleaseCancellation();
	}

	/// <summary>
	/// Asks <paramref name="cancelNative"/> to cancel the request when <paramref name="cancellationToken"/>
	/// is cancelled (immediately when it already is). The task still completes only through
	/// <see cref="Complete"/>, which the native wrapper calls once it released its in-flight
	/// state, so a retry started after the cancelled task completed is never rejected as
	/// already in progress. When <paramref name="cancelNative"/> throws, the request completes
	/// as cancelled at once.
	/// </summary>
	public static void AttachCancellation(long id, CancellationToken cancellationToken, Action<long> cancelNative)
	{
		ArgumentNullException.ThrowIfNull(cancelNative);
		if (!cancellationToken.CanBeCanceled || !Pending.TryGetValue(id, out var request))
			return;

		var registration = cancellationToken.Register(
			static state =>
			{
				var (requestId, cancel) = ((long, Action<long>))state!;
				try
				{
					cancel(requestId);
				}
				catch
				{
					Complete(requestId, new NativeFacebookSignInResult(NativeSignInStatus.Cancelled));
				}
			},
			(id, cancelNative)
		);
		request.Attach(registration);
	}

	/// <summary>
	/// Completes the request with the native result. Returns <see langword="false"/> for an
	/// unknown or already completed request, whose callback is ignored.
	/// </summary>
	public static bool Complete(long id, NativeFacebookSignInResult result)
	{
		if (!Pending.TryRemove(id, out var request))
			return false;

		request.ReleaseCancellation();
		return request.Completion.TrySetResult(result);
	}

	private sealed class Request
	{
		private readonly Lock _gate = new();
		private CancellationTokenRegistration _registration;
		private bool _released;

		public TaskCompletionSource<NativeFacebookSignInResult> Completion { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Attach(CancellationTokenRegistration registration)
		{
			lock (_gate)
			{
				if (!_released)
				{
					_registration = registration;
					return;
				}
			}

			// Completed before the registration was attached.
			registration.Unregister();
		}

		public void ReleaseCancellation()
		{
			CancellationTokenRegistration registration;
			lock (_gate)
			{
				_released = true;
				registration = _registration;
				_registration = default;
			}

			// Unregister never waits for a running cancellation callback, unlike Dispose.
			registration.Unregister();
		}
	}
}
