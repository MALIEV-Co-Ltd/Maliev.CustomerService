using System.Text.Json.Serialization;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Response model for payment term reference data.
/// </summary>
public class PaymentTermResponse
{
    /// <summary>Stable payment term code.</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable payment term label.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Payment term category used for grouping and filtering.</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Description of how the payment term calculates payment timing.</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Guidance describing when this term is typically used.</summary>
    [JsonPropertyName("typicalUse")]
    public string TypicalUse { get; set; } = string.Empty;

    /// <summary>Number of calendar days until payment is due for day-based terms.</summary>
    [JsonPropertyName("dueDays")]
    public int? DueDays { get; set; }

    /// <summary>Early payment discount percentage, when one is available.</summary>
    [JsonPropertyName("discountPercent")]
    public decimal? DiscountPercent { get; set; }

    /// <summary>Number of days the early payment discount is available.</summary>
    [JsonPropertyName("discountDays")]
    public int? DiscountDays { get; set; }

    /// <summary>Whether this payment term is the default for new customers.</summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>Sort order for presenting payment terms.</summary>
    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }
}
