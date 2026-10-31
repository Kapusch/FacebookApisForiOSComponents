namespace Kapusch.Facebook.iOS;

/// <summary>Outcome of <see cref="NativeFacebookLogin.SignInAsync"/>.</summary>
/// <param name="Status">
/// <see cref="NativeSignInStatus.Success"/> always carries <paramref name="AccessToken"/>,
/// <paramref name="AuthenticationToken"/> or both.
/// </param>
/// <param name="AccessToken">
/// Graph API access token of this login (classic login, <see cref="FacebookTrackingMode.Enabled"/>
/// with tracking authorized); <see langword="null"/> with Limited Login.
/// </param>
/// <param name="AuthenticationToken">
/// OIDC authentication token (JWT) of this login, the Limited Login credential. Its
/// <c>nonce</c> claim equals <paramref name="Nonce"/>.
/// </param>
/// <param name="UserId">Facebook user ID of the access token; <see langword="null"/> without one.</param>
/// <param name="Nonce">
/// <c>nonce</c> claim of <paramref name="AuthenticationToken"/>, which equals the nonce passed to
/// <see cref="NativeFacebookLogin.SignInAsync"/> (for Firebase, the SHA-256 of the raw nonce,
/// never the raw nonce itself); <see langword="null"/> without an authentication token.
/// </param>
/// <param name="ErrorCode">
/// Stable code of a <see cref="NativeSignInStatus.Failed"/> result: <c>already_in_progress</c>,
/// <c>missing_nonce</c>, <c>invalid_login_configuration</c>, <c>missing_token</c>,
/// <c>nonce_mismatch</c>, <c>login_feature_not_selected</c>, or <c>&lt;NSError domain&gt;:&lt;code&gt;</c>
/// for an SDK error.
/// </param>
/// <param name="ErrorMessage">Human-readable description of the failure, for logs only.</param>
public sealed record NativeFacebookSignInResult(
	NativeSignInStatus Status,
	string? AccessToken = null,
	string? AuthenticationToken = null,
	string? UserId = null,
	string? Nonce = null,
	string? ErrorCode = null,
	string? ErrorMessage = null
);
