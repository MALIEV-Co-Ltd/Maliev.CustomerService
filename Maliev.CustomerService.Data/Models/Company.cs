using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Company entity for business relationship management
/// Supports segmentation (Retail, Wholesale, Enterprise, Government) and tier classification
/// </summary>
public class Company
{
    /// <summary>
    /// Unique identifier for the company
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Company name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// VAT (Value Added Tax) number with country prefix (e.g., "TH-1234567890")
    /// </summary>
    public string? VatNumber { get; set; }

    /// <summary>
    /// Company registration number
    /// </summary>
    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// Contact email address
    /// </summary>
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number (E.164 format)
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Company segment (Retail, Wholesale, Enterprise, Government)
    /// </summary>
    public string Segment { get; set; } = CustomerSegment.Retail;

    /// <summary>
    /// Company tier (Bronze, Silver, Gold, Platinum, VIP)
    /// </summary>
    public string Tier { get; set; } = CustomerTier.Bronze;

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
