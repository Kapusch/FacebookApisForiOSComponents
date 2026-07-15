namespace Kapusch.Facebook.iOS;

public enum NativeFacebookShareStatus
{
	Success,
	Cancelled,
	Failed,
}

public sealed record NativeFacebookShareResult(
	NativeFacebookShareStatus Status,
	string? ErrorCode = null
);
