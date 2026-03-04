using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Addresses;

/// <summary>
/// Response model for address data
/// </summary>
public class AddressResponse
{
    /// <summary>
    /// Unique identifier for the address
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Type of owner (Customer or Company)
    /// </summary>
    [JsonPropertyName("ownerType")]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the owning entity
    /// </summary>
    [JsonPropertyName("ownerId")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Address type (Billing or Shipping)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default address
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// First line of address
    /// </summary>
    [JsonPropertyName("addressLine1")]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Second line of address
    /// </summary>
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Third line of address
    /// </summary>
    [JsonPropertyName("addressLine3")]
    public string? AddressLine3 { get; set; }

    /// <summary>
    /// District name
    /// </summary>
    [JsonPropertyName("district")]
    public string? District { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or Province name
    /// </summary>
    [JsonPropertyName("stateProvince")]
    public string StateProvince { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country ID
    /// </summary>
    [JsonPropertyName("countryId")]
    public Guid CountryId { get; set; }

    /// <summary>
    /// Name of the recipient (e.g. for shipping)
    /// </summary>
    [JsonPropertyName("recipientName")]
    public string? RecipientName { get; set; }

    /// <summary>
    /// Phone number of the recipient
    /// </summary>
    [JsonPropertyName("recipientPhone")]
    public string? RecipientPhone { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
