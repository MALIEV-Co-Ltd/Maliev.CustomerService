using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Address entity with polymorphic ownership (Customer or Company)
/// Supports both Billing and Shipping address types
/// </summary>
public class Address
{
    /// <summary>
    /// Unique identifier for the address
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of owner (Customer or Company)
    /// </summary>
    public string OwnerType { get; set; } = Entities.OwnerType.Customer;

    /// <summary>
    /// ID of the owning entity (Customer.Id or Company.Id)
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Address type (Billing or Shipping)
    /// </summary>
    public string Type { get; set; } = Entities.AddressType.Billing;

    /// <summary>
    /// Whether this is the default address of its type for the owner
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Friendly place label selected by the user, such as Home, Work, or Other
    /// </summary>
    public string? PlaceLabel { get; set; }

    /// <summary>
    /// Custom place label text when <see cref="PlaceLabel"/> is Other
    /// </summary>
    public string? PlaceLabelOther { get; set; }

    /// <summary>
    /// First line of address
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Second line of address
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Third line of address
    /// </summary>
    public string? AddressLine3 { get; set; }

    /// <summary>
    /// District name (Tambon in Thailand)
    /// </summary>
    public string? District { get; set; }

    /// <summary>
    /// City name (Amphoe in Thailand)
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or Province name (Changwat in Thailand)
    /// </summary>
    public string StateProvince { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country ID (validated via Country Service)
    /// </summary>
    public Guid CountryId { get; set; }

    /// <summary>
    /// Name of the recipient (e.g. for shipping)
    /// </summary>
    public string? RecipientName { get; set; }

    /// <summary>
    /// Phone number of the recipient
    /// </summary>
    public string? RecipientPhone { get; set; }

    /// <summary>
    /// Optional delivery instruction shown to the driver
    /// </summary>
    public string? DriverNote { get; set; }

    /// <summary>
    /// Source used to populate this address, such as Manual, GooglePlace, or GoogleMapPin
    /// </summary>
    public string AddressSource { get; set; } = "Manual";

    /// <summary>
    /// Google Place identifier returned by Places API when available
    /// </summary>
    public string? GooglePlaceId { get; set; }

    /// <summary>
    /// Provider formatted address text returned by Google Maps
    /// </summary>
    public string? FormattedAddress { get; set; }

    /// <summary>
    /// Latitude selected from Google Maps or Places
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude selected from Google Maps or Places
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}
