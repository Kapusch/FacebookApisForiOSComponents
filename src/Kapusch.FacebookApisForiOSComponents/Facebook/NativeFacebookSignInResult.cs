namespace Kapusch.Facebook.iOS;

public sealed record NativeFacebookSignInResult(
	NativeSignInStatus Status,
	string? AccessToken = null,
	string? AuthenticationToken = null,
	string? UserId = null,
	string? Nonce = null,
	string? ErrorCode = null,
	string? ErrorMessage = null
);
