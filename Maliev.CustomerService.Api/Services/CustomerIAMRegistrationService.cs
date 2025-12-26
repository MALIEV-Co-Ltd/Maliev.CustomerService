using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.CustomerService.Api.Authorization;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Background service that registers Customer Service permissions and roles with IAM.
/// Uses the standard IAMRegistrationService base class.
/// </summary>
public class CustomerIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance.</param>
    public CustomerIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<CustomerIAMRegistrationService> logger)
        : base(httpClientFactory, logger, "customer")
    {
    }

    /// <summary>
    /// Gets all permissions for the Customer Service.
    /// </summary>
    /// <returns>Collection of permission registrations.</returns>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return CustomerPermissions.All.Select(p => new PermissionRegistration
        {
            PermissionId = p,
            Description = GetPermissionDescription(p)
        });
    }

    /// <summary>
    /// Gets all predefined roles for the Customer Service.
    /// </summary>
    /// <returns>Collection of role registrations.</returns>
    protected override IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.RoleRegistration> GetPredefinedRoles()
    {
        return CustomerPredefinedRoles.All.Select(r => new Maliev.Aspire.ServiceDefaults.IAM.RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }

    private static string GetPermissionDescription(string permission)
    {
        return permission switch
        {
            CustomerPermissions.CustomersCreate => "Create new customers",
            CustomerPermissions.CustomersRead => "Read customer information",
            CustomerPermissions.CustomersUpdate => "Update customer information",
            CustomerPermissions.CustomersDelete => "Delete customers",
            CustomerPermissions.CustomersList => "List all customers",
            CustomerPermissions.CustomersSearch => "Search customers",
            CustomerPermissions.CompaniesManage => "Manage company information",
            CustomerPermissions.AddressesManage => "Manage customer addresses",
            CustomerPermissions.DocumentsCreate => "Create customer documents",
            CustomerPermissions.DocumentsRead => "Read customer documents",
            CustomerPermissions.DocumentsDelete => "Delete customer documents",
            CustomerPermissions.NotesCreate => "Create internal notes",
            CustomerPermissions.NotesRead => "Read internal notes",
            CustomerPermissions.NotesUpdate => "Update internal notes",
            CustomerPermissions.NotesDelete => "Delete internal notes",
            CustomerPermissions.NdasCreate => "Create NDAs",
            CustomerPermissions.NdasRead => "Read NDAs",
            CustomerPermissions.NdasUpdate => "Update NDAs",
            CustomerPermissions.NdasDelete => "Delete NDAs",
            _ => $"Permission: {permission}"
        };
    }
}