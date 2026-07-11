using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class CustomerControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public CustomerControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task POST_CustomersValidate_WithAccountPermission_ReturnsValidCustomerIdentity()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var anonymousClient = _factory.CreateClient();
        var email = $"portal.{Guid.NewGuid():N}@example.com";

        var registerResponse = await anonymousClient.PostAsJsonAsync("/customer/v1/customers/register", new RegisterCustomerRequest
        {
            FirstName = "Portal",
            LastName = "Customer",
            Email = email,
            RegistrationMethod = "Email",
            Password = "Correct-Horse-1234"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(registered);

        var client = _factory.CreateAuthenticatedClient(
            "auth-service",
            new[] { "roles.customer.account-service" },
            new[] { "customer.accounts.manage" });

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/customers/validate", new ValidateCustomerCredentialsRequest
        {
            Email = email,
            Password = "Correct-Horse-1234"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var validation = await response.Content.ReadFromJsonAsync<ValidateCustomerCredentialsResponse>();
        Assert.NotNull(validation);
        Assert.True(validation!.IsValid);
        Assert.Equal(registered!.Id, validation.CustomerId);
        Assert.Equal(registered.PrincipalId, validation.PrincipalId);
    }

    [Fact]
    public async Task POST_CustomersValidate_WithoutAccountPermission_ReturnsForbidden()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "auth-service",
            new[] { "roles.customer.account-service" },
            permissions: null);

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/customers/validate", new ValidateCustomerCredentialsRequest
        {
            Email = "missing@example.com",
            Password = "Wrong-Password-1234"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task POST_GoogleLinkOrRegister_NonAuthoritativeExistingEmail_ReturnsConflict()
    {
        await _factory.ClearDatabaseAsync();
        using var anonymousClient = _factory.CreateClient();
        var email = $"third.party.{Guid.NewGuid():N}@example.com";
        var registerResponse = await anonymousClient.PostAsJsonAsync(
            "/customer/v1/customers/register",
            new RegisterCustomerRequest
            {
                FirstName = "Existing",
                LastName = "Customer",
                Email = email,
                RegistrationMethod = "Email",
                Password = "Correct-Horse-1234"
            });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        using var client = CreateServiceClient("auth");

        var response = await client.PostAsJsonAsync(
            "/customer/v1/customers/google/link-or-register",
            new LinkOrRegisterGoogleCustomerRequest
            {
                Email = email,
                FirstName = "Different",
                LastName = "Person",
                GoogleSubject = "different-google-subject",
                EmailVerified = true,
                EmailLinkAllowed = false
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("GOOGLE_EMAIL_LINK_REQUIRES_VERIFICATION", error?.Code);
    }

    [Fact]
    public async Task POST_GoogleLinkOrRegister_DifferentPermittedService_ReturnsForbidden()
    {
        await _factory.ClearDatabaseAsync();
        using var client = CreateServiceClient("quote-engine");

        var response = await client.PostAsJsonAsync(
            "/customer/v1/customers/google/link-or-register",
            new LinkOrRegisterGoogleCustomerRequest
            {
                Email = "customer@gmail.com",
                FirstName = "Forged",
                LastName = "Identity",
                GoogleSubject = "forged-google-subject",
                EmailVerified = true,
                EmailLinkAllowed = true
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }


    [Fact]
    public async Task GetByPrincipalId_ReturnsCustomer_WhenExists()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var principalId = Guid.NewGuid();
        var email = $"test.{Guid.NewGuid():N}@example.com";

        // Seed customer with PrincipalId
        using var dbContext = _factory.GetDbContext();
        var customer = new Maliev.CustomerService.Domain.Entities.Customer
        {
            FirstName = "Lookup",
            LastName = "User",
            Email = email,
            PrincipalId = principalId,
            Segment = "Retail",
            Tier = "Bronze"
        };
        await dbContext.Customers.AddAsync(customer);
        await dbContext.CustomerAccounts.AddAsync(new CustomerAccount
        {
            CustomerId = customer.Id,
            PrincipalId = principalId,
            Email = email,
            Status = CustomerAccountStatus.Active,
            EmailVerified = true
        });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/customers/by-principal/{principalId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CustomerResponse>(json);
        Assert.NotNull(result);
        Assert.Equal(customer.Id, result!.Id);
        Assert.Equal(principalId, result.PrincipalId);
        Assert.Equal(email, result.Email);
        using var body = JsonDocument.Parse(json);
        Assert.False(body.RootElement.TryGetProperty("accountStatus", out _));
        Assert.False(body.RootElement.TryGetProperty("accountEmailVerified", out _));
    }

    [Theory]
    [InlineData(CustomerAccountStatus.Active, true)]
    [InlineData(CustomerAccountStatus.Disabled, false)]
    public async Task GetAuthenticationContext_ReturnsNarrowAuthoritativeAccountState_WhenAccountExists(
        string accountStatus,
        bool accountEmailVerified)
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.account-reader" },
            new[] { "customer.accounts.read" });

        var principalId = Guid.NewGuid();
        var email = $"account-state.{Guid.NewGuid():N}@example.com";
        const string profileImageUrl = "https://assets.example.com/customer.png";

        await using var dbContext = _factory.GetDbContext();
        var customer = new Customer
        {
            FirstName = "Account",
            LastName = "State",
            Email = email,
            PrincipalId = principalId,
            Segment = "Retail",
            Tier = "Bronze",
            ProfileImageUrl = profileImageUrl
        };
        dbContext.Customers.Add(customer);
        dbContext.CustomerAccounts.Add(new CustomerAccount
        {
            CustomerId = customer.Id,
            PrincipalId = principalId,
            Email = email,
            Status = accountStatus,
            EmailVerified = accountEmailVerified
        });
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync(
            $"/customer/v1/customers/by-principal/{principalId}/authentication-context");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        var propertyNames = root.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            [
                "accountEmail",
                "accountEmailVerified",
                "accountStatus",
                "customerEmail",
                "customerId",
                "firstName",
                "lastName",
                "name",
                "principalId",
                "profileImageUrl"
            ],
            propertyNames);
        Assert.Equal(customer.Id, root.GetProperty("customerId").GetGuid());
        Assert.Equal(principalId, root.GetProperty("principalId").GetGuid());
        Assert.Equal("Account", root.GetProperty("firstName").GetString());
        Assert.Equal("State", root.GetProperty("lastName").GetString());
        Assert.Equal("Account State", root.GetProperty("name").GetString());
        Assert.Equal(email, root.GetProperty("customerEmail").GetString());
        Assert.Equal(email, root.GetProperty("accountEmail").GetString());
        Assert.Equal(profileImageUrl, root.GetProperty("profileImageUrl").GetString());
        Assert.Equal(accountStatus, root.GetProperty("accountStatus").GetString());
        Assert.Equal(accountEmailVerified, root.GetProperty("accountEmailVerified").GetBoolean());
    }

    [Fact]
    public async Task GetAuthenticationContext_ReturnsCustomerAndAccountEmails_WhenTheyDiffer()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.account-reader" },
            new[] { "customer.accounts.read" });
        var principalId = Guid.NewGuid();
        var customerEmail = $"customer.{Guid.NewGuid():N}@example.com";
        var accountEmail = $"account.{Guid.NewGuid():N}@example.com";

        await using var dbContext = _factory.GetDbContext();
        var customer = new Customer
        {
            FirstName = "Email",
            LastName = "Drift",
            Email = customerEmail,
            PrincipalId = principalId,
            Segment = "Retail",
            Tier = "Bronze"
        };
        dbContext.Customers.Add(customer);
        dbContext.CustomerAccounts.Add(new CustomerAccount
        {
            CustomerId = customer.Id,
            PrincipalId = principalId,
            Email = accountEmail,
            Status = CustomerAccountStatus.Active,
            EmailVerified = true
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync(
            $"/customer/v1/customers/by-principal/{principalId}/authentication-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(customerEmail, body.RootElement.GetProperty("customerEmail").GetString());
        Assert.Equal(accountEmail, body.RootElement.GetProperty("accountEmail").GetString());
    }

    [Fact]
    public async Task GetAuthenticationContext_ReturnsNotFound_WhenAccountDoesNotExist()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.account-reader" },
            new[] { "customer.accounts.read" });
        var principalId = Guid.NewGuid();

        await using var dbContext = _factory.GetDbContext();
        dbContext.Customers.Add(new Customer
        {
            FirstName = "No",
            LastName = "Account",
            Email = $"no-account.{Guid.NewGuid():N}@example.com",
            PrincipalId = principalId,
            Segment = "Retail",
            Tier = "Bronze"
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync(
            $"/customer/v1/customers/by-principal/{principalId}/authentication-context");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("AUTHENTICATION_CONTEXT_NOT_FOUND", error?.Code);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAuthenticationContext_ReturnsNotFound_WhenCustomerIsMissingOrDeleted(
        bool seedDeletedCustomer)
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.account-reader" },
            new[] { "customer.accounts.read" });
        var principalId = Guid.NewGuid();

        if (seedDeletedCustomer)
        {
            await using var dbContext = _factory.GetDbContext();
            var customer = new Customer
            {
                FirstName = "Deleted",
                LastName = "Account",
                Email = $"deleted-account.{Guid.NewGuid():N}@example.com",
                PrincipalId = principalId,
                Segment = "Retail",
                Tier = "Bronze",
                IsDeleted = true
            };
            dbContext.Customers.Add(customer);
            dbContext.CustomerAccounts.Add(new CustomerAccount
            {
                CustomerId = customer.Id,
                PrincipalId = principalId,
                Email = customer.Email,
                Status = CustomerAccountStatus.Active,
                EmailVerified = true
            });
            await dbContext.SaveChangesAsync();
        }

        var response = await client.GetAsync(
            $"/customer/v1/customers/by-principal/{principalId}/authentication-context");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("AUTHENTICATION_CONTEXT_NOT_FOUND", error?.Code);
    }

    [Fact]
    public async Task GetAuthenticationContext_ReturnsForbidden_WhenAccountsReadIsMissing()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var response = await client.GetAsync(
            $"/customer/v1/customers/by-principal/{Guid.NewGuid()}/authentication-context");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByPrincipalId_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });
        var nonExistentPrincipalId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/customer/v1/customers/by-principal/{nonExistentPrincipalId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("NOT_FOUND", error!.Code);
    }

    [Fact]
    public async Task GetByPrincipalId_ReturnsNotFound_WhenCustomerIsDeleted()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var principalId = Guid.NewGuid();

        // Seed deleted customer with PrincipalId
        using var dbContext = _factory.GetDbContext();
        var customer = new Maliev.CustomerService.Domain.Entities.Customer
        {
            FirstName = "Deleted",
            LastName = "User",
            Email = "deleted@example.com",
            PrincipalId = principalId,
            IsDeleted = true,
            Segment = "Retail",
            Tier = "Bronze"
        };
        await dbContext.Customers.AddAsync(customer);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/customers/by-principal/{principalId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByPrincipalId_ReturnsForbidden_WhenMissingPermission()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        // Create client WITHOUT the required permission
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null); // No permissions

        var principalId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/customer/v1/customers/by-principal/{principalId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateServiceClient(string serviceName)
    {
        var token = _factory.CreateTestJwtToken(
            serviceName,
            ["roles.customer.account-service"],
            ["customer.accounts.manage"],
            new Dictionary<string, string>
            {
                ["user_type"] = "service",
                ["service_name"] = serviceName
            });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
