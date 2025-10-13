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
    /// City name
    /// </summary>
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Province/State name
    /// </summary>
    [JsonPropertyName("province")]
    public string Province { get; set; } = string.Empty;

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
    /// Row version for optimistic concurrency control
    /// </summary>
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
