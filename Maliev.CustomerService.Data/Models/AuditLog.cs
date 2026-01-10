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
    /// <summary>Customer actor type.</summary>
    public const string Customer = "Customer";
    /// <summary>Employee actor type.</summary>
    public const string Employee = "Employee";
    /// <summary>System actor type.</summary>
    public const string System = "System";

    /// <summary>All actor types.</summary>
    public static readonly string[] All = { Customer, Employee, System };
}

/// <summary>
/// Audit action enumeration
/// </summary>
public static class AuditAction
{
    /// <summary>Create action.</summary>
    public const string Create = "Create";
    /// <summary>Update action.</summary>
    public const string Update = "Update";
    /// <summary>Delete action.</summary>
    public const string Delete = "Delete";
    /// <summary>Soft delete action.</summary>
    public const string SoftDelete = "SoftDelete";
    /// <summary>Restore action.</summary>
    public const string Restore = "Restore";
    /// <summary>Login action.</summary>
    public const string Login = "Login";
    /// <summary>Logout action.</summary>
    public const string Logout = "Logout";
    /// <summary>Validate credentials action.</summary>
    public const string ValidateCredentials = "ValidateCredentials";

    /// <summary>All audit actions.</summary>
    public static readonly string[] All = { Create, Update, Delete, SoftDelete, Restore, Login, Logout, ValidateCredentials };
}
