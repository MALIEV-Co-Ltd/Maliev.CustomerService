namespace Maliev.CustomerService.Application.DTOs;

/// <summary>
/// Response DTO for tier settings
/// </summary>
public class TierSettingsResponse
{
    public Guid Id { get; set; }
    public string TierName { get; set; } = string.Empty;
    public decimal MinPurchaseValue { get; set; }
    public int MinOrderCount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? FreeShippingMinOrder { get; set; }
    public decimal? CoinRewardPercentage { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public uint xmin { get; set; }
}

/// <summary>
/// Request DTO for creating/updating tier settings
/// </summary>
public class TierSettingsRequest
{
    public string TierName { get; set; } = string.Empty;
    public decimal MinPurchaseValue { get; set; }
    public int MinOrderCount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? FreeShippingMinOrder { get; set; }
    public decimal? CoinRewardPercentage { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
