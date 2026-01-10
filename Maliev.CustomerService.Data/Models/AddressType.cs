namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Address type constants (Billing or Shipping)
/// </summary>
public static class AddressType
{
    /// <summary>
    /// Billing address type.
    /// </summary>
    public const string Billing = "Billing";

    /// <summary>
    /// Shipping address type.
    /// </summary>
    public const string Shipping = "Shipping";

    /// <summary>
    /// All address types.
    /// </summary>
    public static readonly string[] All = { Billing, Shipping };
}
