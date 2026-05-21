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
    /// Friendly place label selected by the user, such as Home, Work, or Other
    /// </summary>
    [MaxLength(50)]
    [JsonPropertyName("placeLabel")]
    public string? PlaceLabel { get; set; }

    /// <summary>
    /// Custom place label text when placeLabel is Other
    /// </summary>
    [MaxLength(100)]
    [JsonPropertyName("placeLabelOther")]
    public string? PlaceLabelOther { get; set; }

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

    /// <summary>
    /// Optional delivery instruction shown to the driver
    /// </summary>
    [JsonPropertyName("driverNote")]
    [MaxLength(500)]
    public string? DriverNote { get; set; }

    /// <summary>
    /// Source used to populate this address, such as Manual, GooglePlace, or GoogleMapPin
    /// </summary>
    [JsonPropertyName("addressSource")]
    [MaxLength(50)]
    public string? AddressSource { get; set; }

    /// <summary>
    /// Google Place identifier returned by Places API when available
    /// </summary>
    [JsonPropertyName("googlePlaceId")]
    [MaxLength(255)]
    public string? GooglePlaceId { get; set; }

    /// <summary>
    /// Provider formatted address text returned by Google Maps
    /// </summary>
    [JsonPropertyName("formattedAddress")]
    [MaxLength(500)]
    public string? FormattedAddress { get; set; }

    /// <summary>
    /// Latitude selected from Google Maps or Places
    /// </summary>
    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude selected from Google Maps or Places
    /// </summary>
    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control
    /// </summary>
    [Required(ErrorMessage = "xmin is required for updates")]
    [JsonPropertyName("xmin")]
    public uint xmin { get; set; }
}
