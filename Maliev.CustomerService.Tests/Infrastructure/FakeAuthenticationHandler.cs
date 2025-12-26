using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maliev.CustomerService.Tests.Infrastructure;

/// <summary>
/// Fake authentication handler for testing authorized endpoints
/// Allows setting custom claims and roles per test
/// </summary>
public class FakeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "FakeScheme";
    public const string DefaultUserId = "test-user-id";
    public const string DefaultUsername = "test-user";

    public FakeAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if test has set custom claims in headers
        var claims = new List<Claim>
        {
            new Claim("sub", DefaultUserId),
            new Claim("name", DefaultUsername),
            new Claim(ClaimTypes.NameIdentifier, DefaultUserId),
            new Claim(ClaimTypes.Name, DefaultUsername)
        };

        // Allow tests to override user ID via header
        if (Context.Request.Headers.TryGetValue("X-Test-User-Id", out var userId))
        {
            claims.Add(new Claim("sub", userId.ToString()));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        }

        // Allow tests to override username via header
        if (Context.Request.Headers.TryGetValue("X-Test-Username", out var username))
        {
            claims.Add(new Claim("name", username.ToString()));
            claims.Add(new Claim(ClaimTypes.Name, username.ToString()));
        }

        // Allow tests to add roles via header (comma-separated)
        if (Context.Request.Headers.TryGetValue("X-Test-Roles", out var roles))
        {
            var roleList = roles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var role in roleList)
            {
                claims.Add(new Claim("role", role.Trim()));
                claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
            }
        }
        else
        {
            // Default to Customer role if no roles specified
            claims.Add(new Claim("role", "Customer"));
            claims.Add(new Claim(ClaimTypes.Role, "Customer"));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Helper class to configure test authentication headers
/// </summary>
public static class TestAuthenticationHelper
{
    public static Dictionary<string, string> CreateAuthHeaders(
        string? userId = null,
        string? username = null,
        string[]? roles = null)
    {
        var headers = new Dictionary<string, string>();

        if (userId != null)
        {
            headers["X-Test-User-Id"] = userId;
        }

        if (username != null)
        {
            headers["X-Test-Username"] = username;
        }

        if (roles != null && roles.Length > 0)
        {
            headers["X-Test-Roles"] = string.Join(",", roles);
        }

        return headers;
    }

    public static Dictionary<string, string> CreateEmployeeHeaders()
    {
        return CreateAuthHeaders(
            userId: "employee-user-id",
            username: "employee-user",
            roles: new[] { "Employee" }
        );
    }

    public static Dictionary<string, string> CreateManagerHeaders()
    {
        return CreateAuthHeaders(
            userId: "manager-user-id",
            username: "manager-user",
            roles: new[] { "Manager" }
        );
    }

    public static Dictionary<string, string> CreateAdminHeaders()
    {
        return CreateAuthHeaders(
            userId: "admin-user-id",
            username: "admin-user",
            roles: new[] { "Admin" }
        );
    }

    public static Dictionary<string, string> CreateCustomerHeaders(string? customerId = null)
    {
        return CreateAuthHeaders(
            userId: customerId ?? "customer-user-id",
            username: "customer-user",
            roles: new[] { "Customer" }
        );
    }
}
