namespace Maliev.CustomerService.Api.Models.IAM;

/// <summary>
/// Detailed response model for a principal from IAM
/// </summary>
public record PrincipalResponse
{
    /// <summary>Unique identifier of the principal.</summary>
    public Guid PrincipalId { get; init; }

    /// <summary>Type of principal (user, service_account, system).</summary>
    public string PrincipalType { get; init; } = string.Empty;

    /// <summary>Email address of the principal.</summary>
    public string? Email { get; init; }

    /// <summary>Display name of the principal.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Name of the linked service.</summary>
    public string? LinkedService { get; init; }

    /// <summary>Unique identifier in the linked service.</summary>
    public Guid? LinkedEntityId { get; init; }

    /// <summary>Whether the principal is active.</summary>
    public bool IsActive { get; init; }

    /// <summary>Date and time the principal was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Date and time the principal was last updated.</summary>
    public DateTime UpdatedAt { get; init; }
}
