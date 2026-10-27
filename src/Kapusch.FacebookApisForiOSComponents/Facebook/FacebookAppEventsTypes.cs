namespace Kapusch.Facebook.iOS;

/// <summary>When the SDK sends queued app events (<c>FBSDKAppEventsFlushBehavior</c>).</summary>
public enum FacebookAppEventsFlushBehavior
{
	/// <summary>Flush periodically, at an event-count threshold and on app reactivation (SDK default).</summary>
	Auto = 0,

	/// <summary>Only flush on <see cref="FacebookAppEvents.Flush"/>. Events are persisted meanwhile.</summary>
	ExplicitOnly = 1,
}

/// <summary>
/// Advanced-matching user data passed to <see cref="FacebookAppEvents.SetUserData"/>. Pass raw
/// values: the SDK normalizes and hashes (SHA-256) each one on the device before persisting or
/// sending it. <see cref="FacebookAppEvents.SetUserData"/> replaces every field at once, so a
/// <see langword="null"/> property clears that field (same as the Android package).
/// </summary>
/// <remarks>
/// Advanced matching sends personal data (hashed) to Meta. Collect it only with a legal basis and
/// clear it with <see cref="FacebookAppEvents.ClearUserData"/> when that basis ends.
/// </remarks>
public sealed record FacebookAppEventsUserData
{
	/// <summary>Email address.</summary>
	public string? Email { get; init; }

	/// <summary>First name.</summary>
	public string? FirstName { get; init; }

	/// <summary>Last name.</summary>
	public string? LastName { get; init; }

	/// <summary>Phone number, including the country code.</summary>
	public string? Phone { get; init; }

	/// <summary>Date of birth, formatted <c>YYYYMMDD</c>.</summary>
	public string? DateOfBirth { get; init; }

	/// <summary>Gender: <c>m</c> or <c>f</c>.</summary>
	public string? Gender { get; init; }

	/// <summary>City.</summary>
	public string? City { get; init; }

	/// <summary>State or province code.</summary>
	public string? State { get; init; }

	/// <summary>Zip or postal code.</summary>
	public string? Zip { get; init; }

	/// <summary>Country code (ISO 3166-1 alpha-2).</summary>
	public string? Country { get; init; }

	/// <summary>An ID representing the user in the app's own systems (<c>external_id</c>).</summary>
	public string? ExternalId { get; init; }
}

/// <summary>Data processing options for <see cref="FacebookAppEvents.SetDataProcessingOptions"/>.</summary>
public static class FacebookDataProcessingOptions
{
	/// <summary>Limited Data Use. Pass an empty array to explicitly not enable it.</summary>
	public const string LimitedDataUse = "LDU";
}
