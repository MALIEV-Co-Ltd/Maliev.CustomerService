namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Address type constants (Billing or Shipping)
/// </summary>
public static class AddressType
{
    public const string Billing = "Billing";
    public const string Shipping = "Shipping";

    public static readonly string[] All = { Billing, Shipping };
}
