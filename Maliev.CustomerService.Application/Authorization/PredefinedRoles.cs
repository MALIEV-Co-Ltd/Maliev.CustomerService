namespace Maliev.CustomerService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Customer Service.
/// </summary>
public static class CustomerPredefinedRoles
{
    public const string Admin = "roles.customer.admin";
    public const string Sales = "roles.customer.sales";
    public const string Support = "roles.customer.support";
    public const string Viewer = "roles.customer.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Customer Administrator with full access",
            new[]
            {
                CustomerPermissions.CompanyRead,
                CustomerPermissions.CompanyManage,
                CustomerPermissions.CompanyWrite,
                CustomerPermissions.CustomerCreate,
                CustomerPermissions.CustomerRead,
                CustomerPermissions.CustomerUpdate,
                CustomerPermissions.CustomerDelete,
                CustomerPermissions.CustomerList,
                CustomerPermissions.CustomerSearch,
                CustomerPermissions.AddressManage,
                CustomerPermissions.DocumentCreate,
                CustomerPermissions.DocumentRead,
                CustomerPermissions.DocumentDelete,
                CustomerPermissions.NoteCreate,
                CustomerPermissions.NoteRead,
                CustomerPermissions.NoteUpdate,
                CustomerPermissions.NoteDelete,
                CustomerPermissions.NdaCreate,
                CustomerPermissions.NdaRead,
                CustomerPermissions.NdaUpdate,
                CustomerPermissions.NdaDelete,
                CustomerPermissions.TierRead,
                CustomerPermissions.TierManage,
            }
        ),
        (
            Sales,
            "Sales role with customer and document access",
            new[]
            {
                CustomerPermissions.CompanyRead,
                CustomerPermissions.CompanyManage,
                CustomerPermissions.CompanyWrite,
                CustomerPermissions.CustomerCreate,
                CustomerPermissions.CustomerRead,
                CustomerPermissions.CustomerUpdate,
                CustomerPermissions.CustomerList,
                CustomerPermissions.CustomerSearch,
                CustomerPermissions.AddressManage,
                CustomerPermissions.DocumentCreate,
                CustomerPermissions.DocumentRead,
                CustomerPermissions.NoteCreate,
                CustomerPermissions.NoteRead,
                CustomerPermissions.NdaCreate,
                CustomerPermissions.NdaRead,
                CustomerPermissions.TierRead,
            }
        ),
        (
            Support,
            "Support role with customer read and note access",
            new[]
            {
                CustomerPermissions.CompanyRead,
                CustomerPermissions.CustomerRead,
                CustomerPermissions.CustomerList,
                CustomerPermissions.CustomerSearch,
                CustomerPermissions.AddressManage,
                CustomerPermissions.DocumentRead,
                CustomerPermissions.NoteCreate,
                CustomerPermissions.NoteRead,
                CustomerPermissions.NoteUpdate,
                CustomerPermissions.NdaRead,
                CustomerPermissions.TierRead,
            }
        ),
        (
            Viewer,
            "Customer Viewer with read-only access",
            new[]
            {
                CustomerPermissions.CompanyRead,
                CustomerPermissions.CustomerRead,
                CustomerPermissions.CustomerList,
                CustomerPermissions.AddressManage,
                CustomerPermissions.DocumentRead,
                CustomerPermissions.NoteRead,
                CustomerPermissions.NdaRead,
                CustomerPermissions.TierRead,
            }
        ),
    };
}
