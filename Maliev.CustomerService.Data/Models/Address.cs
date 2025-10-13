using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Data.Models;

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
    public string OwnerType { get; set; } = Models.OwnerType.Customer;

    /// <summary>
    /// ID of the owning entity (Customer.Id or Company.Id)
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Address type (Billing or Shipping)
    /// </summary>
    public string Type { get; set; } = Models.AddressType.Billing;

    /// <summary>
    /// First line of address
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Second line of address (optional)
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// City name
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Province/State name
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// Postal/ZIP code
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Country ID (validated via Country Service)
    /// </summary>
    public Guid CountryId { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
