using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Durable customer-scoped memory used by Make Studio and customer-facing assistants.
/// </summary>
public class CustomerMemory
{
    /// <summary>Unique memory identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Customer that owns this memory.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Memory category, such as make_studio_preference.</summary>
    [Required]
    [MaxLength(80)]
    public string MemoryType { get; set; } = string.Empty;

    /// <summary>Stable memory key within the memory type.</summary>
    [Required]
    [MaxLength(120)]
    public string Key { get; set; } = string.Empty;

    /// <summary>Customer-safe memory value.</summary>
    [Required]
    [MaxLength(1200)]
    public string Value { get; set; } = string.Empty;

    /// <summary>Confidence score from 0.0 to 1.0.</summary>
    public decimal Confidence { get; set; }

    /// <summary>Source that observed or updated this memory.</summary>
    [Required]
    [MaxLength(80)]
    public string Source { get; set; } = "unknown";

    /// <summary>Number of times this memory has been observed or reinforced.</summary>
    public int HitCount { get; set; } = 1;

    /// <summary>Most recent observation timestamp.</summary>
    public DateTime LastObservedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Record creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Record last update timestamp.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Owning customer navigation property.</summary>
    public Customer? Customer { get; set; }
}
