namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// NDA status constants for lifecycle management
/// </summary>
public static class NDAStatus
{
    /// <summary>NDA is in draft.</summary>
    public const string Draft = "Draft";
    /// <summary>NDA is signed.</summary>
    public const string Signed = "Signed";
    /// <summary>NDA is expired.</summary>
    public const string Expired = "Expired";
    /// <summary>NDA is revoked.</summary>
    public const string Revoked = "Revoked";

    /// <summary>All NDA statuses.</summary>
    public static readonly string[] All = { Draft, Signed, Expired, Revoked };

    /// <summary>
    /// Terminal states that cannot transition to other states
    /// </summary>
    public static readonly string[] TerminalStates = { Expired, Revoked };
}
