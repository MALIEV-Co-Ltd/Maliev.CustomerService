namespace Maliev.CustomerService.Application.DTOs;

/// <summary>
/// Response DTO for company with tier information
/// </summary>
public class CompanyWithTierResponse
{
    /// <summary>
    /// Unique identifier of the company.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Name of the company.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Current tier level (e.g., "Classic", "Silver", "Gold").
    /// </summary>
    public string Tier { get; set; } = string.Empty;
    /// <summary>
    /// Total purchase value for the current year.
    /// </summary>
    public decimal CurrentYearPurchaseValue { get; set; }
    /// <summary>
    /// Total number of orders placed in the current year.
    /// </summary>
    public int CurrentYearOrderCount { get; set; }
    /// <summary>
    /// Timestamp when the tier was last calculated.
    /// </summary>
    public DateTime? TierCalculatedAt { get; set; }
    /// <summary>
    /// Discount percentage applied to orders based on tier.
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
    /// Progress information towards the next tier.
    /// </summary>
    public TierProgressResponse? NextTierProgress { get; set; }
}

/// <summary>
/// Progress information towards next tier
/// </summary>
public class TierProgressResponse
{
    /// <summary>
    /// Name of the next tier level.
    /// </summary>
    public string NextTierName { get; set; } = string.Empty;
    /// <summary>
    /// Required purchase value to reach the next tier.
    /// </summary>
    public decimal RequiredPurchaseValue { get; set; }
    /// <summary>
    /// Required order count to reach the next tier.
    /// </summary>
    public int RequiredOrderCount { get; set; }
    /// <summary>
    /// Current purchase value towards the next tier.
    /// </summary>
    public decimal CurrentPurchaseValue { get; set; }
    /// <summary>
    /// Current order count towards the next tier.
    /// </summary>
    public int CurrentOrderCount { get; set; }
    /// <summary>
    /// Progress percentage towards purchase value requirement (0-100).
    /// </summary>
    public decimal PurchaseValueProgress { get; set; }
    /// <summary>
    /// Progress percentage towards order count requirement (0-100).
    /// </summary>
    public decimal OrderCountProgress { get; set; }
}
