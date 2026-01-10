using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Customer entity with segmentation, localization, and communication preferences
/// </summary>
public class Customer
{
    /// <summary>
    /// Unique customer identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the central IAM principal ID
    /// </summary>
    [Required]
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// Customer's first name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's last name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Customer's email address (unique per active customer)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Customer's phone number (E.164 format)
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Customer segmentation: Retail, Wholesale, Enterprise, Government
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Segment { get; set; } = "Retail";

    /// <summary>
    /// Customer tier: Bronze, Silver, Gold, Platinum, VIP
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Tier { get; set; } = "Bronze";

    /// <summary>
    /// Preferred language (ISO 639-1 format, e.g., "en", "th")
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string PreferredLanguage { get; set; } = "en";

    /// <summary>
    /// Timezone (IANA format, e.g., "Asia/Bangkok")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Timezone { get; set; } = "UTC";

    /// <summary>
    /// Communication preferences stored as JSON (email_opt_in, sms_opt_in, etc.)
    /// </summary>
    public string? CommunicationPreferences { get; set; }

    /// <summary>
    /// Optional link to Company entity
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Record creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Row version for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Customer segment enumeration
/// </summary>
public static class CustomerSegment
{
    /// <summary>Retail segment.</summary>
    public const string Retail = "Retail";
    /// <summary>Wholesale segment.</summary>
    public const string Wholesale = "Wholesale";
    /// <summary>Enterprise segment.</summary>
    public const string Enterprise = "Enterprise";
    /// <summary>Government segment.</summary>
    public const string Government = "Government";

    /// <summary>All customer segments.</summary>
    public static readonly string[] All = { Retail, Wholesale, Enterprise, Government };
}

/// <summary>
/// Customer tier enumeration
/// </summary>
public static class CustomerTier
{
    /// <summary>Bronze tier.</summary>
    public const string Bronze = "Bronze";
    /// <summary>Silver tier.</summary>
    public const string Silver = "Silver";
    /// <summary>Gold tier.</summary>
    public const string Gold = "Gold";
    /// <summary>Platinum tier.</summary>
    public const string Platinum = "Platinum";
    /// <summary>VIP tier.</summary>
    public const string VIP = "VIP";

    /// <summary>All customer tiers.</summary>
    public static readonly string[] All = { Bronze, Silver, Gold, Platinum, VIP };
}
