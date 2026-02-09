using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Addresses;

/// <summary>
/// Request model for updating an existing address
/// </summary>
public class UpdateAddressRequest
{
    /// <summary>
    /// Address type (Billing or Shipping)
    /// </summary>
    [MaxLength(50)]
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this is the default address
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool? IsDefault { get; set; }

    /// <summary>
    /// First line of address
    /// </summary>
    [MaxLength(255)]
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Second line of address
    /// </summary>
    [MaxLength(255)]
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Third line of address
    /// </summary>
    [MaxLength(255)]
    [JsonPropertyName("addressLine3")]
    public string? AddressLine3 { get; set; }

    /// <summary>
    /// District name
    /// </summary>
    [MaxLength(100)]
    [JsonPropertyName("district")]
    public string? District { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    [MaxLength(100)]
    [JsonPropertyName("city")]
    public string? City { get; set; }

    /// <summary>
    /// State or Province name
    /// </summary>
    [MaxLength(100)]
    [JsonPropertyName("stateProvince")]
    public string? StateProvince { get; set; }

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [MaxLength(20)]
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country ID (validated via Country Service)
    /// </summary>
    [JsonPropertyName("countryId")]
    public Guid? CountryId { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "Version is required for updates")]
    [JsonPropertyName("version")]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
