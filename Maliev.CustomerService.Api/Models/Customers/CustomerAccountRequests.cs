using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Request to validate customer email/password credentials.
/// </summary>
public sealed class ValidateCustomerCredentialsRequest
{
    /// <summary>Customer email address.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Customer password.</summary>
    [Required]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response from customer credential validation.
/// </summary>
public sealed class ValidateCustomerCredentialsResponse
{
    /// <summary>Whether the credentials are valid.</summary>
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    /// <summary>Canonical CustomerService customer identifier.</summary>
    [JsonPropertyName("customerId")]
    public Guid? CustomerId { get; set; }

    /// <summary>Central IAM principal identifier.</summary>
    [JsonPropertyName("principalId")]
    public Guid? PrincipalId { get; set; }

    /// <summary>Customer email address.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>Customer display name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Preferred language.</summary>
    [JsonPropertyName("preferredLanguage")]
    public string? PreferredLanguage { get; set; }

    /// <summary>Failure reason for invalid credentials.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Request to link or register a Google customer account.
/// </summary>
public sealed class LinkOrRegisterGoogleCustomerRequest
{
    /// <summary>Customer email address.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>First name from Google profile.</summary>
    [Required]
    [MaxLength(100)]
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name from Google profile.</summary>
    [Required]
    [MaxLength(100)]
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Google OpenID Connect subject claim.</summary>
    [Required]
    [MaxLength(255)]
    [JsonPropertyName("googleSubject")]
    public string GoogleSubject { get; set; } = string.Empty;

    /// <summary>Whether Google reports the email as verified.</summary>
    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }

    /// <summary>Preferred language.</summary>
    [MaxLength(10)]
    [JsonPropertyName("preferredLanguage")]
    public string PreferredLanguage { get; set; } = "th";

    /// <summary>Timezone.</summary>
    [MaxLength(50)]
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "Asia/Bangkok";
}

/// <summary>
/// Response for customer account session linkage.
/// </summary>
public sealed class CustomerAccountSessionResponse
{
    /// <summary>Canonical CustomerService customer identifier.</summary>
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }

    /// <summary>Central IAM principal identifier.</summary>
    [JsonPropertyName("principalId")]
    public Guid PrincipalId { get; set; }

    /// <summary>Customer email address.</summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Customer display name.</summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Preferred language.</summary>
    [JsonPropertyName("preferredLanguage")]
    public string PreferredLanguage { get; set; } = "th";

    /// <summary>Google subject claim when linked.</summary>
    [JsonPropertyName("googleSubject")]
    public string? GoogleSubject { get; set; }
}

/// <summary>
/// Request to start password reset.
/// </summary>
public sealed class PasswordResetRequest
{
    /// <summary>Customer email address.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response for password reset request.
/// </summary>
public sealed class PasswordResetResponse
{
    /// <summary>Whether the request was accepted.</summary>
    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }

    /// <summary>Reset token for notification delivery by the caller.</summary>
    [JsonPropertyName("resetToken")]
    public string? ResetToken { get; set; }
}

/// <summary>
/// Request to confirm password reset.
/// </summary>
public sealed class ConfirmPasswordResetRequest
{
    /// <summary>Customer email address.</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Password reset token.</summary>
    [Required]
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>New password.</summary>
    [Required]
    [MinLength(12)]
    [MaxLength(256)]
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Response for password reset confirmation.
/// </summary>
public sealed class ConfirmPasswordResetResponse
{
    /// <summary>Whether the reset was accepted.</summary>
    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }

    /// <summary>Canonical customer identifier for downstream session revocation.</summary>
    [JsonPropertyName("customerId")]
    public Guid? CustomerId { get; set; }
}
