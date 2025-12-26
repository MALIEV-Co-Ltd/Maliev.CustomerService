namespace Maliev.CustomerService.Api.Authorization;

/// <summary>Represents a role registration request for the IAM service.</summary>
public record RoleRegistration
{
    /// <summary>The unique identifier for the role (GCP format: roles.{service}.{role-name}).</summary>
    public required string RoleId { get; init; }

    /// <summary>The display name of the role.</summary>
    public required string RoleName { get; init; }

    /// <summary>A description of the role's purpose.</summary>
    public required string Description { get; init; }

    /// <summary>The list of permissions assigned to this role.</summary>
    public required string[] Permissions { get; init; }
}

/// <summary>
/// Defines predefined roles for the Customer Service.
/// Roles follow the GCP format: roles.{service}.{role-name}
/// </summary>
public static class CustomerPredefinedRoles
{
    /// <summary>Full access to all customer operations.</summary>
    public static readonly RoleRegistration Admin = new()
    {
        RoleId = "roles.customer.admin",
        RoleName = "Customer Administrator",
        Description = "Full access to all customer operations",
        Permissions = CustomerPermissions.All
    };

    /// <summary>Manage customers, companies, addresses (no document/note deletion).</summary>
    public static readonly RoleRegistration Manager = new()
    {
        RoleId = "roles.customer.manager",
        RoleName = "Customer Manager",
        Description = "Manage customers, companies, and addresses",
        Permissions = new[]
        {
            CustomerPermissions.CustomersCreate,
            CustomerPermissions.CustomersRead,
            CustomerPermissions.CustomersUpdate,
            CustomerPermissions.CustomersDelete,
            CustomerPermissions.CustomersList,
            CustomerPermissions.CustomersSearch,
            CustomerPermissions.CompaniesManage,
            CustomerPermissions.AddressesManage,
            CustomerPermissions.DocumentsCreate,
            CustomerPermissions.DocumentsRead,
            // No DocumentsDelete permission
            CustomerPermissions.NotesCreate,
            CustomerPermissions.NotesRead,
            CustomerPermissions.NotesUpdate,
            // No NotesDelete permission
            CustomerPermissions.NdasCreate,
            CustomerPermissions.NdasRead,
            CustomerPermissions.NdasUpdate
            // No NdasDelete permission
        }
    };

    /// <summary>Create/update customers, view documents/notes.</summary>
    public static readonly RoleRegistration Representative = new()
    {
        RoleId = "roles.customer.representative",
        RoleName = "Customer Representative",
        Description = "Create and update customer data, view documents and notes",
        Permissions = new[]
        {
            CustomerPermissions.CustomersCreate,
            CustomerPermissions.CustomersRead,
            CustomerPermissions.CustomersUpdate,
            CustomerPermissions.CustomersList,
            CustomerPermissions.CustomersSearch,
            CustomerPermissions.DocumentsRead,
            CustomerPermissions.NotesRead,
            CustomerPermissions.NdasRead
        }
    };

    /// <summary>Read-only access to customer data.</summary>
    public static readonly RoleRegistration Viewer = new()
    {
        RoleId = "roles.customer.viewer",
        RoleName = "Customer Viewer",
        Description = "Read-only access to customer data",
        Permissions = new[]
        {
            CustomerPermissions.CustomersRead,
            CustomerPermissions.CustomersList,
            CustomerPermissions.CustomersSearch,
            CustomerPermissions.DocumentsRead,
            CustomerPermissions.NotesRead,
            CustomerPermissions.NdasRead
        }
    };

    /// <summary>All predefined roles.</summary>
    public static readonly RoleRegistration[] All = new[]
    {
        Admin, Manager, Representative, Viewer
    };
}
