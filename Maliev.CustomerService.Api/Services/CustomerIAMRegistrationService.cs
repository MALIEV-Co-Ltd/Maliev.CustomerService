using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.CustomerService.Api.Authorization;

namespace Maliev.CustomerService.Api.Services;

/// <summary>
/// Registers Customer Service permissions and roles with IAM via RabbitMQ.
/// </summary>
public class CustomerIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomerIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public CustomerIAMRegistrationService(
        IConfiguration configuration,
        ILogger<CustomerIAMRegistrationService> logger)
        : base(configuration, logger, "customer")
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return CustomerPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <inheritdoc />
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return CustomerPredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}
