namespace Maliev.CustomerService.Application.DTOs;

/// <summary>
/// Response DTO for tier settings
/// </summary>
public class TierSettingsResponse
{
    /// <summary>
    /// Unique identifier of the tier settings record.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Name of the tier (e.g., "Classic", "Silver", "Gold").
    /// </summary>
    public string TierName { get; set; } = string.Empty;
    /// <summary>
    /// Minimum purchase value required to qualify for this tier.
    /// </summary>
    public decimal MinPurchaseValue { get; set; }
    /// <summary>
    /// Minimum order count required to qualify for this tier.
    /// </summary>
    public int MinOrderCount { get; set; }
    /// <summary>
    /// Discount percentage applied to orders for this tier.
    /// </summary>
    public decimal DiscountPercentage { get; set; }
    /// <summary>
    /// Minimum order value for free shipping, if applicable.
    /// </summary>
    public decimal? FreeShippingMinOrder { get; set; }
    /// <summary>
    /// Percentage of order value awarded as coins, if applicable.
    /// </summary>
    public decimal? CoinRewardPercentage { get; set; }
    /// <summary>
    /// Date from which these tier settings are valid.
    /// </summary>
    public DateTime ValidFrom { get; set; }
    /// <summary>
    /// Date until which these tier settings are valid, if applicable.
    /// </summary>
    public DateTime? ValidTo { get; set; }
    /// <summary>
    /// Row version for optimistic concurrency.
    /// </summary>
    public uint xmin { get; set; }
}

/// <summary>
/// Request DTO for creating/updating tier settings
/// </summary>
public class TierSettingsRequest
{
    /// <summary>
    /// Name of the tier.
    /// </summary>
    public string TierName { get; set; } = string.Empty;
    /// <summary>
    /// Minimum purchase value required for this tier.
    /// </summary>
    public decimal MinPurchaseValue { get; set; }
    /// <summary>
    /// Minimum order count required for this tier.
    /// </summary>
    public int MinOrderCount { get; set; }
    /// <summary>
    /// Discount percentage for this tier.
    /// </summary>
    public decimal DiscountPercentage { get; set; }
    /// <summary>
    /// Minimum order value for free shipping.
    /// </summary>
    public decimal? FreeShippingMinOrder { get; set; }
    /// <summary>
    /// Percentage of order value awarded as coins.
    /// </summary>
    public decimal? CoinRewardPercentage { get; set; }
    /// <summary>
    /// Date from which these settings take effect.
    /// </summary>
    public DateTime ValidFrom { get; set; }
    /// <summary>
    /// Date until which these settings remain valid.
    /// </summary>
    public DateTime? ValidTo { get; set; }
}
