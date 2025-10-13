namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// NDA status constants for lifecycle management
/// </summary>
public static class NDAStatus
{
    public const string Draft = "Draft";
    public const string Signed = "Signed";
    public const string Expired = "Expired";
    public const string Revoked = "Revoked";

    public static readonly string[] All = { Draft, Signed, Expired, Revoked };

    /// <summary>
    /// Terminal states that cannot transition to other states
    /// </summary>
    public static readonly string[] TerminalStates = { Expired, Revoked };
}
