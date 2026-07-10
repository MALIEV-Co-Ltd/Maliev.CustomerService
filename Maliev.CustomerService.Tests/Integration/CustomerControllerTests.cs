using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Customers;
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
        using var client = _factory.CreateAuthenticatedClient(
            "auth-service",
            ["roles.customer.account-service"],
            ["customer.accounts.manage"]);

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
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/customers/by-principal/{principalId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(result);
        Assert.Equal(customer.Id, result!.Id);
        Assert.Equal(principalId, result.PrincipalId);
        Assert.Equal(email, result.Email);
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
}
