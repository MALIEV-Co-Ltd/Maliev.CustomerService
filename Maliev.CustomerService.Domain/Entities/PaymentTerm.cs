using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Reference data describing payment terms available for customer profiles.
/// </summary>
public class PaymentTerm
{
    /// <summary>
    /// Stable payment term code.
    /// </summary>
    [Key]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable payment term label.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of calendar days until payment is due.
    /// </summary>
    public int DueDays { get; set; }

    /// <summary>
    /// Whether this payment term is the default for new customers.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Sort order for presenting payment terms.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Built-in payment term values.
/// </summary>
public static class PaymentTerms
{
    /// <summary>Due immediately on receipt.</summary>
    public const string DueOnReceipt = "Due on receipt";

    /// <summary>Payment due in 15 days.</summary>
    public const string Net15 = "Net 15";

    /// <summary>Payment due in 30 days.</summary>
    public const string Net30 = "Net 30";

    /// <summary>Payment due in 45 days.</summary>
    public const string Net45 = "Net 45";

    /// <summary>All built-in payment terms.</summary>
    public static readonly string[] All = [DueOnReceipt, Net15, Net30, Net45];
}
