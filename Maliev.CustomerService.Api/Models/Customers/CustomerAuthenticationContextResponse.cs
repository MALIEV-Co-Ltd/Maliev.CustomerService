using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Narrow authoritative customer and portal-account context for authentication consumers.
/// </summary>
public sealed class CustomerAuthenticationContextResponse
{
    /// <summary>Canonical CustomerService customer identifier.</summary>
    [JsonPropertyName("customerId")]
    public Guid CustomerId { get; set; }

    /// <summary>Central IAM principal identifier shared by the customer and portal account.</summary>
    [JsonPropertyName("principalId")]
    public Guid PrincipalId { get; set; }

    /// <summary>Customer first name.</summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Customer last name.</summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Customer display name.</summary>
    [JsonPropertyName("name")]
    public string Name => $"{FirstName} {LastName}".Trim();

    /// <summary>Canonical customer profile email.</summary>
    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>Portal account login email.</summary>
    [JsonPropertyName("accountEmail")]
    public string AccountEmail { get; set; } = string.Empty;

    /// <summary>Customer profile image URL.</summary>
    [JsonPropertyName("profileImageUrl")]
    public string? ProfileImageUrl { get; set; }

    /// <summary>Authoritative portal account status.</summary>
    [JsonPropertyName("accountStatus")]
    public string AccountStatus { get; set; } = string.Empty;

    /// <summary>Whether the portal account email is verified.</summary>
    [JsonPropertyName("accountEmailVerified")]
    public bool AccountEmailVerified { get; set; }
}
