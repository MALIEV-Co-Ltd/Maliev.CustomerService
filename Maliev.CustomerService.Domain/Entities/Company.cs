using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Domain.Entities;

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
    public string Segment { get; set; } = "Retail";

    /// <summary>
    /// Company tier (Classic, Silver, Gold)
    /// </summary>
    public string Tier { get; set; } = "Classic";

    /// <summary>
    /// Year-to-date purchase value in THB
    /// </summary>
    public decimal CurrentYearPurchaseValue { get; set; }

    /// <summary>
    /// Year-to-date order count
    /// </summary>
    public int CurrentYearOrderCount { get; set; }

    /// <summary>
    /// Timestamp when tier was last calculated
    /// </summary>
    public DateTime? TierCalculatedAt { get; set; }

    /// <summary>
    /// Full company name in Thai (from BDEX registry)
    /// </summary>
    public string? FullNameTh { get; set; }

    /// <summary>
    /// Company registration date (from BDEX registry)
    /// </summary>
    public DateTime? RegistrationDate { get; set; }

    /// <summary>
    /// Company status code from BDEX (1=Active, 5=Liquidated, 8=Vacant)
    /// </summary>
    public string? CompanyStatus { get; set; }

    /// <summary>
    /// Company status description in Thai
    /// </summary>
    public string? CompanyStatusNameTh { get; set; }

    /// <summary>
    /// Type of business entity code (3=Partnership, 5=Limited Company, etc.)
    /// </summary>
    public string? CompanyTypeCode { get; set; }

    /// <summary>
    /// Business objectives (semicolon-separated from BDEX registry)
    /// </summary>
    public string? BusinessObjectives { get; set; }

    /// <summary>
    /// Whether company data was verified from BDEX registry
    /// </summary>
    public bool IsVerifiedFromBdex { get; set; }

    /// <summary>
    /// Date when company was verified from BDEX
    /// </summary>
    public DateTime? BdexVerificationDate { get; set; }

    /// <summary>
    /// Stock symbol for publicly listed companies
    /// </summary>
    public string? StockSymbol { get; set; }

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
