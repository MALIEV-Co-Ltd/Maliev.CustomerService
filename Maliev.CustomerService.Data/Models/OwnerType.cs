namespace Maliev.CustomerService.Data.Models;

/// <summary>
/// Owner type constants for polymorphic address ownership (Customer or Company)
/// </summary>
public static class OwnerType
{
    public const string Customer = "Customer";
    public const string Company = "Company";

    public static readonly string[] All = { Customer, Company };
}
