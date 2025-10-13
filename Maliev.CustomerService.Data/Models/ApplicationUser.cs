using Microsoft.AspNetCore.Identity;

namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Custom Identity user with customer linkage and activity tracking
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Optional link to Customer entity (for customer self-service accounts)
    /// </summary>
    public Guid? LinkedCustomerId { get; set; }

    /// <summary>
    /// Indicates if the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last successful login timestamp (updated by /validate endpoint)
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last account update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
