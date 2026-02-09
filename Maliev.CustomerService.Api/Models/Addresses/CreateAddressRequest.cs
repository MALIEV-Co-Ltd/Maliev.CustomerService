using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Addresses;

/// <summary>
/// Request model for creating a new address with polymorphic ownership
/// </summary>
public class CreateAddressRequest
{
    /// <summary>
    /// Type of owner (Customer or Company)
    /// </summary>
    [Required(ErrorMessage = "Owner type is required")]
    [MaxLength(50)]
    [JsonPropertyName("ownerType")]
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the owning entity (Customer.Id or Company.Id)
    /// </summary>
    [Required(ErrorMessage = "Owner ID is required")]
    [JsonPropertyName("ownerId")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Address type (Billing or Shipping)
    /// </summary>
    [Required(ErrorMessage = "Address type is required")]
    [MaxLength(50)]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default address
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// First line of address
    /// </summary>
    [Required(ErrorMessage = "Address line 1 is required")]
    [MaxLength(255)]
    [JsonPropertyName("addressLine1")]
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Second line of address (optional)
    /// </summary>
    [MaxLength(255)]
    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Third line of address (optional)
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
    [Required(ErrorMessage = "City is required")]
    [MaxLength(100)]
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or Province name
    /// </summary>
    [Required(ErrorMessage = "State/Province is required")]
    [MaxLength(100)]
    [JsonPropertyName("stateProvince")]
    public string StateProvince { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    [Required(ErrorMessage = "Postal code is required")]
    [MaxLength(20)]
    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country ID (validated via Country Service)
    /// </summary>
    [Required(ErrorMessage = "Country ID is required")]
    [JsonPropertyName("countryId")]
    public Guid CountryId { get; set; }

    /// <summary>
    /// Name of the recipient (e.g. for shipping)
    /// </summary>
    [JsonPropertyName("recipientName")]
    [MaxLength(200)]
    public string? RecipientName { get; set; }

    /// <summary>
    /// Phone number of the recipient
    /// </summary>
    [JsonPropertyName("recipientPhone")]
    [MaxLength(20)]
    public string? RecipientPhone { get; set; }
}
