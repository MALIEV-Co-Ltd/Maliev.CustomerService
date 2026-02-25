namespace Maliev.CustomerService.Application.DTOs;

/// <summary>
/// Response DTO for company with tier information
/// </summary>
public class CompanyWithTierResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public decimal CurrentYearPurchaseValue { get; set; }
    public int CurrentYearOrderCount { get; set; }
    public DateTime? TierCalculatedAt { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? FreeShippingMinOrder { get; set; }
    public decimal? CoinRewardPercentage { get; set; }
    public TierProgressResponse? NextTierProgress { get; set; }
}

/// <summary>
/// Progress information towards next tier
/// </summary>
public class TierProgressResponse
{
    public string NextTierName { get; set; } = string.Empty;
    public decimal RequiredPurchaseValue { get; set; }
    public int RequiredOrderCount { get; set; }
    public decimal CurrentPurchaseValue { get; set; }
    public int CurrentOrderCount { get; set; }
    public decimal PurchaseValueProgress { get; set; }
    public decimal OrderCountProgress { get; set; }
}
