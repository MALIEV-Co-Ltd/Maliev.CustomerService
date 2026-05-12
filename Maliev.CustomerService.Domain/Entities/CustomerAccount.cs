using System.ComponentModel.DataAnnotations;

namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Customer portal account linked to the canonical customer profile.
/// </summary>
public class CustomerAccount
{
    /// <summary>
    /// Unique account identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Canonical CustomerService customer identifier.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Central IAM principal identifier.
    /// </summary>
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// Login email address.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password hash for email/password sign-in.
    /// </summary>
    [MaxLength(1000)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Google OpenID Connect subject claim.
    /// </summary>
    [MaxLength(255)]
    public string? GoogleSubject { get; set; }

    /// <summary>
    /// Account status.
    /// </summary>
    [Required]
    [MaxLength(40)]
    public string Status { get; set; } = CustomerAccountStatus.Active;

    /// <summary>
    /// Whether the account email has been verified.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Password reset token hash.
    /// </summary>
    [MaxLength(128)]
    public string? PasswordResetTokenHash { get; set; }

    /// <summary>
    /// Password reset token expiration timestamp.
    /// </summary>
    public DateTimeOffset? PasswordResetTokenExpiresAtUtc { get; set; }

    /// <summary>
    /// Last successful sign-in timestamp.
    /// </summary>
    public DateTimeOffset? LastLoginAtUtc { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Record update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Linked customer profile.
    /// </summary>
    public Customer? Customer { get; set; }
}

/// <summary>
/// Customer account status values.
/// </summary>
public static class CustomerAccountStatus
{
    /// <summary>Account can sign in.</summary>
    public const string Active = "Active";

    /// <summary>Account is invited but has not set credentials yet.</summary>
    public const string InvitationPending = "InvitationPending";

    /// <summary>Account is disabled.</summary>
    public const string Disabled = "Disabled";
}
