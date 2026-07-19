using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Api.Models.Customers;

/// <summary>
/// Request to observe or reinforce a customer-scoped memory.
/// </summary>
public class CustomerMemoryObserveRequest
{
    /// <summary>Memory category, such as make_studio_preference.</summary>
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string MemoryType { get; set; } = string.Empty;

    /// <summary>Stable memory key within the memory type.</summary>
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Key { get; set; } = string.Empty;

    /// <summary>Customer-safe memory value.</summary>
    [Required]
    [StringLength(1200, MinimumLength = 1)]
    public string Value { get; set; } = string.Empty;

    /// <summary>Confidence score from 0.0 to 1.0.</summary>
    [Range(typeof(decimal), "0", "1")]
    public decimal Confidence { get; set; } = 0.5m;

    /// <summary>Source that observed or updated this memory.</summary>
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string Source { get; set; } = "unknown";
}

/// <summary>
/// Customer memory response.
/// </summary>
public class CustomerMemoryResponse
{
    /// <summary>Unique memory identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Customer that owns this memory.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Memory category.</summary>
    public string MemoryType { get; set; } = string.Empty;

    /// <summary>Stable memory key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Customer-safe memory value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Confidence score from 0.0 to 1.0.</summary>
    public decimal Confidence { get; set; }

    /// <summary>Source that most recently observed this memory.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Number of times this memory has been observed or reinforced.</summary>
    public int HitCount { get; set; }

    /// <summary>Most recent observation timestamp.</summary>
    public DateTime LastObservedAt { get; set; }

    /// <summary>Record creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Record last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Query response for customer-scoped memories.
/// </summary>
public class CustomerMemoryQueryResponse
{
    /// <summary>Customer that owns the returned memories.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Normalized search query.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>Maximum number of returned memories.</summary>
    public int Limit { get; set; }

    /// <summary>Matching customer memories.</summary>
    public List<CustomerMemoryResponse> Items { get; set; } = [];
}
