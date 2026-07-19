namespace Maliev.CustomerService.Domain.Entities;

/// <summary>
/// Owner type constants for polymorphic address ownership (Customer or Company)
/// </summary>
public static class OwnerType
{
    /// <summary>Customer owner type.</summary>
    public const string Customer = "Customer";
    /// <summary>Company owner type.</summary>
    public const string Company = "Company";

    /// <summary>All owner types.</summary>
    public static readonly string[] All = { Customer, Company };
}
