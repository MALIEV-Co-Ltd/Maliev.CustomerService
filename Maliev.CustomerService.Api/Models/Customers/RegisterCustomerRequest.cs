using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Request model for customer self-registration.
/// Simplified compared to <see cref="CreateCustomerRequest"/> — customers provide
/// only the essential fields. Segment and Tier default to "Retail" and "Bronze".
/// </summary>
public class RegisterCustomerRequest
{
    /// <summary>Email address (used as login identifier).</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>First name.</summary>
    [Required]
    [MaxLength(100)]
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Last name.</summary>
    [Required]
    [MaxLength(100)]
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Phone number (optional).</summary>
    [MaxLength(20)]
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>Registration method: "Google" or "Email".</summary>
    [Required]
    [MaxLength(20)]
    [JsonPropertyName("registrationMethod")]
    public string RegistrationMethod { get; set; } = "Email";

    /// <summary>Preferred language code (optional, defaults to "th").</summary>
    [MaxLength(10)]
    [JsonPropertyName("preferredLanguage")]
    public string PreferredLanguage { get; set; } = "th";

    /// <summary>Timezone (optional, defaults to "Asia/Bangkok").</summary>
    [MaxLength(50)]
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "Asia/Bangkok";

    /// <summary>Google SSO subject claim (optional — set when registering via Google).</summary>
    [JsonPropertyName("googleSub")]
    public string? GoogleSub { get; set; }

    /// <summary>Password for email/password customer accounts.</summary>
    [MinLength(12)]
    [MaxLength(256)]
    [JsonPropertyName("password")]
    public string? Password { get; set; }
}
