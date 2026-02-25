using Maliev.CustomerService.Domain.Authorization;

namespace Maliev.CustomerService.Domain.Authorization;

/// <summary>
/// Defines predefined roles for the Customer Service.
/// Roles follow the GCP format: roles.{service}.{role-name}
/// </summary>
public static class CustomerPredefinedRoles
{
    /// <summary>Full access to all customer operations.</summary>
    public const string Admin = "roles.customer.admin";
    /// <summary>Manage customers, companies, and addresses.</summary>
    public const string Manager = "roles.customer.manager";
    /// <summary>Create and update customer data, view documents and notes.</summary>
    public const string Representative = "roles.customer.representative";
    /// <summary>Read-only access to customer data.</summary>
    public const string Viewer = "roles.customer.viewer";

    /// <summary>
    /// Collection of all predefined roles for the Customer Service.
    /// </summary>
    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (Admin, "Full access to all customer operations", CustomerPermissions.All),

        (Manager, "Manage customers, companies, and addresses", new[]
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
            CustomerPermissions.NotesCreate,
            CustomerPermissions.NotesRead,
            CustomerPermissions.NotesUpdate,
            CustomerPermissions.NdasCreate,
            CustomerPermissions.NdasRead,
            CustomerPermissions.NdasUpdate,
            CustomerPermissions.TiersRead,
            CustomerPermissions.TiersManage
        }),

        (Representative, "Create and update customer data, view documents and notes", new[]
        {
            CustomerPermissions.CustomersCreate,
            CustomerPermissions.CustomersRead,
            CustomerPermissions.CustomersUpdate,
            CustomerPermissions.CustomersList,
            CustomerPermissions.CustomersSearch,
            CustomerPermissions.DocumentsRead,
            CustomerPermissions.NotesRead,
            CustomerPermissions.NdasRead,
            CustomerPermissions.CompaniesRead
        }),

        (Viewer, "Read-only access to customer data", new[]
        {
            CustomerPermissions.CustomersRead,
            CustomerPermissions.CustomersList,
            CustomerPermissions.CustomersSearch,
            CustomerPermissions.DocumentsRead,
            CustomerPermissions.NotesRead,
            CustomerPermissions.NdasRead,
            CustomerPermissions.CompaniesRead,
            CustomerPermissions.TiersRead
        })
    };
}
