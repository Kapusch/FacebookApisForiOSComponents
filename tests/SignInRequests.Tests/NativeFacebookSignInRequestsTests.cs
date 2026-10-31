using Xunit;

namespace Kapusch.Facebook.iOS.Tests;

// The registry is static: keep every test in this class so they never run in parallel.
public sealed class NativeFacebookSignInRequestsTests
{
	private static readonly NativeFacebookSignInResult Success = new(
		NativeSignInStatus.Success,
		AuthenticationToken: "token",
		Nonce: "hashed"
	);

	private static readonly NativeFacebookSignInResult Cancelled = new(NativeSignInStatus.Cancelled);

	[Fact]
	public async Task Complete_DeliversTheNativeResultOnce()
	{
		var id = NativeFacebookSignInRequests.Begin(out var task);

		Assert.True(NativeFacebookSignInRequests.Complete(id, Success));
		Assert.False(NativeFacebookSignInRequests.Complete(id, Cancelled));

		Assert.Same(Success, await task);
	}

	[Fact]
	public void Begin_GivesEveryRequestANewId()
	{
		var first = NativeFacebookSignInRequests.Begin(out _);
		var second = NativeFacebookSignInRequests.Begin(out _);

		Assert.True(second > first);
		NativeFacebookSignInRequests.Complete(first, Cancelled);
		NativeFacebookSignInRequests.Complete(second, Cancelled);
	}

	[Fact]
	public async Task Complete_IgnoresAnUnknownRequest()
	{
		var id = NativeFacebookSignInRequests.Begin(out var task);
		NativeFacebookSignInRequests.Complete(id, Success);

		Assert.False(NativeFacebookSignInRequests.Complete(id + 1_000_000, Cancelled));
		Assert.False(NativeFacebookSignInRequests.Complete(0, Cancelled));
		Assert.Same(Success, await task);
	}

	[Fact]
	public async Task Cancellation_AsksNativeAndCompletesOnlyThroughTheNativeCallback()
	{
		using var cts = new CancellationTokenSource();
		var id = NativeFacebookSignInRequests.Begin(out var task);
		var cancelled = new List<long>();
		NativeFacebookSignInRequests.AttachCancellation(id, cts.Token, cancelled.Add);

		cts.Cancel();

		Assert.Equal([id], cancelled);
		// The native wrapper has not released its in-flight state yet.
		Assert.False(task.IsCompleted);

		Assert.True(NativeFacebookSignInRequests.Complete(id, Cancelled));
		Assert.Equal(NativeSignInStatus.Cancelled, (await task).Status);
	}

	[Fact]
	public async Task LateNativeCallback_AfterCancellationIsIgnored()
	{
		using var cts = new CancellationTokenSource();
		var id = NativeFacebookSignInRequests.Begin(out var task);
		NativeFacebookSignInRequests.AttachCancellation(id, cts.Token, _ => { });

		cts.Cancel();
		NativeFacebookSignInRequests.Complete(id, Cancelled);

		Assert.False(NativeFacebookSignInRequests.Complete(id, Success));
		Assert.Equal(NativeSignInStatus.Cancelled, (await task).Status);
	}

	[Fact]
	public void Cancellation_AfterCompletionDoesNotReachNative()
	{
		using var cts = new CancellationTokenSource();
		var id = NativeFacebookSignInRequests.Begin(out _);
		var calls = 0;
		NativeFacebookSignInRequests.AttachCancellation(id, cts.Token, _ => calls++);

		NativeFacebookSignInRequests.Complete(id, Success);
		cts.Cancel();

		Assert.Equal(0, calls);
	}

	[Fact]
	public void AlreadyCancelledToken_AsksNativeImmediately()
	{
		var id = NativeFacebookSignInRequests.Begin(out _);
		var cancelled = new List<long>();

		NativeFacebookSignInRequests.AttachCancellation(id, new CancellationToken(true), cancelled.Add);

		Assert.Equal([id], cancelled);
		NativeFacebookSignInRequests.Complete(id, Cancelled);
	}

	[Fact]
	public void AttachCancellation_AfterASynchronousNativeCallbackDoesNothing()
	{
		using var cts = new CancellationTokenSource();
		var id = NativeFacebookSignInRequests.Begin(out _);
		// The native start rejected the request (for example already_in_progress) before returning.
		NativeFacebookSignInRequests.Complete(id, new NativeFacebookSignInResult(NativeSignInStatus.Failed));
		var calls = 0;

		NativeFacebookSignInRequests.AttachCancellation(id, cts.Token, _ => calls++);
		cts.Cancel();

		Assert.Equal(0, calls);
	}

	[Fact]
	public async Task FailingNativeCancellation_CompletesTheRequestAsCancelled()
	{
		using var cts = new CancellationTokenSource();
		var id = NativeFacebookSignInRequests.Begin(out var task);
		NativeFacebookSignInRequests.AttachCancellation(
			id,
			cts.Token,
			_ => throw new EntryPointNotFoundException("kfb_facebook_signin_cancel")
		);

		cts.Cancel();

		Assert.Equal(NativeSignInStatus.Cancelled, (await task).Status);
		Assert.False(NativeFacebookSignInRequests.Complete(id, Success));
	}

	[Fact]
	public void Abandon_ForgetsTheRequest()
	{
		var before = NativeFacebookSignInRequests.PendingCount;
		var id = NativeFacebookSignInRequests.Begin(out var task);
		Assert.Equal(before + 1, NativeFacebookSignInRequests.PendingCount);

		NativeFacebookSignInRequests.Abandon(id);

		Assert.Equal(before, NativeFacebookSignInRequests.PendingCount);
		Assert.False(NativeFacebookSignInRequests.Complete(id, Success));
		Assert.False(task.IsCompleted);
	}
}
