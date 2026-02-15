using System.Security.Claims;

namespace Maliev.CustomerService.Api.Authorization;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract actor information.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts actor ID and actor type from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>A tuple containing the actor ID and actor type.</returns>
    public static (string ActorId, string ActorType) GetActorInfo(this ClaimsPrincipal principal)
    {
        // Extract user ID from JWT claims (typically "sub" claim)
        var actorId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? "unknown";

        // Determine actor type from roles or specific user_type claim
        var userType = principal.FindFirst("user_type")?.Value;
        if (!string.IsNullOrEmpty(userType))
        {
            return (actorId, userType);
        }

        // Determine actor type from role claims
        // Employee role = Employee actor type, otherwise Customer
        // Updated for GCP-style roles: roles.customer.{role-name}
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var internalRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Employee", "Manager", "Admin",
            CustomerPredefinedRoles.Admin,
            CustomerPredefinedRoles.Manager,
            CustomerPredefinedRoles.Representative
        };
        var isInternal = roles.Any(internalRoles.Contains);

        var actorType = isInternal ? "Employee" : "Customer";
        return (actorId, actorType);
    }
    /// <summary>
    /// Extracts actor name from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The actor name or "Unknown".</returns>
    public static string GetActorName(this ClaimsPrincipal principal)
    {
        var givenName = principal.FindFirst(ClaimTypes.GivenName)?.Value ?? principal.FindFirst("given_name")?.Value;
        var familyName = principal.FindFirst(ClaimTypes.Surname)?.Value ?? principal.FindFirst("family_name")?.Value;

        if (!string.IsNullOrEmpty(givenName) || !string.IsNullOrEmpty(familyName))
        {
            return $"{givenName} {familyName}".Trim();
        }

        var name = principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("name")?.Value
            ?? principal.FindFirst("preferred_username")?.Value;

        if (!string.IsNullOrEmpty(name)) return name;

        return principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value ?? "Unknown";
    }
}
