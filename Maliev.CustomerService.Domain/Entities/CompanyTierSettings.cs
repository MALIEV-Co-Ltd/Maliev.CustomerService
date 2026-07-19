using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Configurable tier thresholds and benefits
/// </summary>
public class CompanyTierSettings
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tier level name (Classic, Silver, Gold)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string TierName { get; set; } = string.Empty;

    /// <summary>
    /// Minimum YTD purchase value for this tier
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal MinPurchaseValue { get; set; }

    /// <summary>
    /// Minimum YTD order count for this tier
    /// </summary>
    public int MinOrderCount { get; set; }

    /// <summary>
    /// Discount percentage applied to orders (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Minimum order value for free shipping
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FreeShippingMinOrder { get; set; }

    /// <summary>
    /// Coin reward percentage (0-100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? CoinRewardPercentage { get; set; }

    /// <summary>
    /// Settings effective from
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Settings effective until (null = indefinite)
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
