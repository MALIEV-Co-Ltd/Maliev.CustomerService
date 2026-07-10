namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Indicates that a third-party Google email cannot claim an existing customer account without reauthentication.
/// </summary>
public sealed class GoogleEmailLinkVerificationRequiredException : InvalidOperationException
{
    /// <summary>Stable API error code.</summary>
    public const string StableCode = "GOOGLE_EMAIL_LINK_REQUIRES_VERIFICATION";

    /// <summary>Initializes the exception with a safe account-linking message.</summary>
    public GoogleEmailLinkVerificationRequiredException()
        : base("Sign in to the existing MALIEV account before linking this Google identity.")
    {
    }

    /// <summary>Gets the stable API error code.</summary>
    public string Code => StableCode;
}
