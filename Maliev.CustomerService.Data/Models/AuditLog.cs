using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Audit log for tracking all mutations in the system
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique audit log entry identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the actor who performed the action
    /// </summary>
    [Required]
    public string ActorId { get; set; } = string.Empty;

    /// <summary>
    /// Type of actor: Customer, Employee, System
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ActorType { get; set; } = "System";

    /// <summary>
    /// Action performed (Create, Update, Delete, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected (Customer, Address, Company, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity affected
    /// </summary>
    [Required]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// JSON representation of changed fields
    /// </summary>
    public string? ChangedFields { get; set; }

    /// <summary>
    /// JSON representation of previous values (for updates)
    /// </summary>
    public string? PreviousValues { get; set; }
}

/// <summary>
/// Actor type enumeration
/// </summary>
public static class ActorType
{
    public const string Customer = "Customer";
    public const string Employee = "Employee";
    public const string System = "System";

    public static readonly string[] All = { Customer, Employee, System };
}

/// <summary>
/// Audit action enumeration
/// </summary>
public static class AuditAction
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string SoftDelete = "SoftDelete";
    public const string Restore = "Restore";
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string ValidateCredentials = "ValidateCredentials";

    public static readonly string[] All = { Create, Update, Delete, SoftDelete, Restore, Login, Logout, ValidateCredentials };
}
