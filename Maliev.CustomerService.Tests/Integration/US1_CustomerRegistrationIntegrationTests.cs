using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.IAM;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

/// <summary>
/// Integration tests for User Story 1 - Customer Registration and Basic Information Management
/// Tests all 8 acceptance scenarios using real HTTP requests
/// </summary>
[Collection("Database Collection")]
public class US1_CustomerRegistrationIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _databaseFixture;
    private TestWebApplicationFactory _factory = null!;
    private string _testId = null!;

    private static readonly string[] EmployeeRoles = { "roles.customer.representative" };
    private static readonly string[] CustomerRoles = { "roles.customer.viewer" };

    public US1_CustomerRegistrationIntegrationTests(TestDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        _testId = Guid.NewGuid().ToString("N")[..8];
        _factory = new TestWebApplicationFactory();
        await _factory.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    private string UniqueEmail(string prefix) => $"{prefix}.{_testId}@example.com";

    /// <summary>
    /// Scenario 1: Create customer with valid data → verify returned data
    /// </summary>
    [Fact]
    public async Task Scenario1_CreateCustomer_WithValidData_ReturnsCreatedCustomer()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles);
        var email = UniqueEmail("john.doe");
        var request = new
        {
            firstName = "John",
            lastName = "Doe",
            email,
            phone = "+6621234567",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/customers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        Assert.NotEqual(Guid.Empty, customer!.Id);
        Assert.Equal("John", customer.FirstName);
        Assert.Equal("Doe", customer.LastName);
        Assert.Equal(email, customer.Email);
        Assert.Equal("+6621234567", customer.Phone);
        Assert.Equal("Retail", customer.Segment);
        Assert.Equal("Bronze", customer.Tier);
        Assert.Equal("en", customer.PreferredLanguage);
        Assert.Equal("Asia/Bangkok", customer.Timezone);
        Assert.True(Math.Abs((customer.CreatedAt - DateTime.UtcNow).TotalSeconds) < 5);
        Assert.False(customer.IsDeleted);
    }

    /// <summary>
    /// Scenario 2: Create customer with duplicate email → verify validation error
    /// </summary>
    [Fact]
    public async Task Scenario2_CreateCustomer_WithDuplicateEmail_ReturnsConflictError()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles);
        var duplicateEmail = UniqueEmail("jane.smith");
        var request1 = new
        {
            firstName = "Jane",
            lastName = "Smith",
            email = duplicateEmail,
            phone = "+6621234567",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var request2 = new
        {
            firstName = "John",
            lastName = "Doe",
            email = duplicateEmail, // Duplicate email
            phone = "+6629999999",
            segment = "Wholesale",
            tier = "Silver",
            preferredLanguage = "th",
            timezone = "Asia/Bangkok"
        };

        var response1 = await client.PostAsJsonAsync("/customer/v1/customers", request1);
        var response2 = await client.PostAsJsonAsync("/customer/v1/customers", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

        var error = await response2.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("DUPLICATE_EMAIL", error!.Code);
        Assert.Contains("already exists", error.Message);
    }

    /// <summary>
    /// Scenario 3: Retrieve customer by ID → verify complete data
    /// </summary>
    [Fact]
    public async Task Scenario3_RetrieveCustomer_ById_ReturnsCompleteCustomerData()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles);
        var email = UniqueEmail("alice.johnson");
        var createRequest = new
        {
            firstName = "Alice",
            lastName = "Johnson",
            email,
            phone = "+6625555555",
            segment = "Enterprise",
            tier = "Gold",
            preferredLanguage = "th",
            timezone = "Asia/Bangkok",
            communicationPreferences = new Dictionary<string, object>
            {
                { "email_opt_in", true },
                { "sms_opt_in", false }
            }
        };

        var createResponse = await client.PostAsJsonAsync("/customer/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Act
        var getResponse = await client.GetAsync($"/customer/v1/customers/{createdCustomer!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var customer = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        Assert.Equal(createdCustomer.Id, customer!.Id);
        Assert.Equal("Alice", customer.FirstName);
        Assert.Equal("Johnson", customer.LastName);
        Assert.Equal(email, customer.Email);
        Assert.Equal("+6625555555", customer.Phone);
        Assert.Equal("Enterprise", customer.Segment);
        Assert.Equal("Gold", customer.Tier);
        Assert.Equal("th", customer.PreferredLanguage);
        Assert.Equal("Asia/Bangkok", customer.Timezone);
        Assert.NotNull(customer.CommunicationPreferences);
        Assert.True(customer.CommunicationPreferences!.ContainsKey("email_opt_in"));
        Assert.True(Math.Abs((customer.CreatedAt - DateTime.UtcNow).TotalSeconds) < 5);
        Assert.True(Math.Abs((customer.UpdatedAt - DateTime.UtcNow).TotalSeconds) < 5);
    }

    /// <summary>
    /// Scenario 4: Update customer (employee actor) → verify changes and audit log
    /// </summary>
    [Fact]
    public async Task Scenario4_UpdateCustomer_ByEmployee_RecordsEmployeeActorInAudit()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles);
        var email = UniqueEmail("bob.wilson");
        var createRequest = new
        {
            firstName = "Bob",
            lastName = "Wilson",
            email,
            phone = "+6621111111",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var createResponse = await client.PostAsJsonAsync("/customer/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var updateRequest = new
        {
            phone = "+6622222222",
            lastName = "Wilson-Updated",
            version = createdCustomer!.Version
        };

        // Act
        var updateResponse = await client.PatchAsJsonAsync($"/customer/v1/customers/{createdCustomer.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(updatedCustomer);
        Assert.Equal("+6622222222", updatedCustomer!.Phone);
        Assert.Equal("Wilson-Updated", updatedCustomer.LastName);
        Assert.Equal("Bob", updatedCustomer.FirstName); // Unchanged
        Assert.True(updatedCustomer.UpdatedAt > updatedCustomer.CreatedAt);

        // Verify audit log entry exists for employee actor
        using var dbContext = _factory.GetDbContext();
        var auditLogs = await dbContext.AuditLogs
            .Where(a => a.EntityId == createdCustomer.Id.ToString() && a.Action == "Update")
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        Assert.Contains(auditLogs, a => a.ActorType == "Employee");
    }

    /// <summary>
    /// Scenario 5: Update customer (customer self-service) → verify actorType = "Customer"
    /// </summary>
    [Fact]
    public async Task Scenario5_UpdateCustomer_BySelf_RecordsCustomerActorInAudit()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var employeeClient = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles);
        var email = UniqueEmail("charlie.brown");
        var createRequest = new
        {
            firstName = "Charlie",
            lastName = "Brown",
            email,
            phone = "+6623333333",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var createResponse = await employeeClient.PostAsJsonAsync("/customer/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create customer client with the customer's ID
        var customerClient = _factory.CreateAuthenticatedClient(createdCustomer!.Id.ToString(), CustomerRoles);
        var updateRequest = new
        {
            phone = "+6624444444",
            version = createdCustomer.Version
        };

        // Act
        var updateResponse = await customerClient.PatchAsJsonAsync($"/customer/v1/customers/{createdCustomer.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(updatedCustomer);
        Assert.Equal("+6624444444", updatedCustomer!.Phone);

        // Verify audit log entry exists for customer actor
        using var dbContext = _factory.GetDbContext();
        var auditLogs = await dbContext.AuditLogs
            .Where(a => a.EntityId == createdCustomer.Id.ToString() && a.Action == "Update")
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
        Assert.Equal("Customer", auditLogs.First().ActorType);
    }

    /// <summary>
    /// Scenario 6: Update preferred_language and timezone → verify changes
    /// </summary>
    [Fact]
    public async Task Scenario6_UpdateCustomer_PreferredLanguageAndTimezone_ChangesApplied()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles);
        var email = UniqueEmail("diana.prince");
        var createRequest = new
        {
            firstName = "Diana",
            lastName = "Prince",
            email,
            phone = "+6627777777",
            segment = "Wholesale",
            tier = "Silver",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var createResponse = await client.PostAsJsonAsync("/customer/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var updateRequest = new
        {
            preferredLanguage = "th",
            timezone = "Asia/Singapore",
            version = createdCustomer!.Version
        };

        // Act
        var updateResponse = await client.PatchAsJsonAsync($"/customer/v1/customers/{createdCustomer.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(updatedCustomer);
        Assert.Equal("th", updatedCustomer!.PreferredLanguage);
        Assert.Equal("Asia/Singapore", updatedCustomer.Timezone);
        Assert.True(updatedCustomer.UpdatedAt > updatedCustomer.CreatedAt);

        // Verify changes in audit log
        using var dbContext = _factory.GetDbContext();
        var auditLog = await dbContext.AuditLogs
            .Where(a => a.EntityId == createdCustomer.Id.ToString() && a.Action == "Update")
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);
    }

    /// <summary>
    /// Scenario 7: Update communication_preferences → verify JSONB storage
    /// </summary>
    [Fact]
    public async Task Scenario7_UpdateCustomer_CommunicationPreferences_StoredAsJsonb()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles);
        var email = UniqueEmail("edward.norton");
        var createRequest = new
        {
            firstName = "Edward",
            lastName = "Norton",
            email,
            phone = "+6628888888",
            segment = "Government",
            tier = "Platinum",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var createResponse = await client.PostAsJsonAsync("/customer/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var updateRequest = new
        {
            communicationPreferences = new Dictionary<string, object>
            {
                { "email_opt_in", true },
                { "sms_opt_in", false },
                { "marketing_opt_in", true }
            },
            version = createdCustomer!.Version
        };

        // Act
        var updateResponse = await client.PatchAsJsonAsync($"/customer/v1/customers/{createdCustomer.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(updatedCustomer);
        Assert.NotNull(updatedCustomer!.CommunicationPreferences);
        Assert.True(updatedCustomer.CommunicationPreferences!.ContainsKey("email_opt_in"));
        Assert.True(updatedCustomer.CommunicationPreferences.ContainsKey("sms_opt_in"));
        Assert.True(updatedCustomer.CommunicationPreferences.ContainsKey("marketing_opt_in"));
        Assert.Equal("True", updatedCustomer.CommunicationPreferences["email_opt_in"]!.ToString());
        Assert.Equal("False", updatedCustomer.CommunicationPreferences["sms_opt_in"]!.ToString());
        Assert.Equal("True", updatedCustomer.CommunicationPreferences["marketing_opt_in"]!.ToString());

        // Verify the data is persisted in the database
        var getResponse = await client.GetAsync($"/customer/v1/customers/{createdCustomer.Id}");
        var retrievedCustomer = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(retrievedCustomer!.CommunicationPreferences);
        Assert.Equal(3, retrievedCustomer.CommunicationPreferences!.Count);
    }

    /// <summary>
    /// Scenario 8: Soft delete customer → verify isDeleted flag and exclusion from queries
    /// </summary>
    [Fact]
    public async Task Scenario8_SoftDeleteCustomer_MarkedAsDeletedAndExcludedFromQueries()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles);

        // Create two customers
        var request1 = new
        {
            firstName = "Frank",
            lastName = "Castle",
            email = UniqueEmail("frank.castle"),
            phone = "+6621111111",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var request2 = new
        {
            firstName = "Grace",
            lastName = "Hopper",
            email = UniqueEmail("grace.hopper"),
            phone = "+6622222222",
            segment = "Enterprise",
            tier = "Gold",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var response1 = await client.PostAsJsonAsync("/customer/v1/customers", request1);
        var customer1 = await response1.Content.ReadFromJsonAsync<CustomerResponse>();

        var response2 = await client.PostAsJsonAsync("/customer/v1/customers", request2);
        var customer2 = await response2.Content.ReadFromJsonAsync<CustomerResponse>();

        // Act - Soft delete the first customer
        var deleteResponse = await client.DeleteAsync($"/customer/v1/customers/{customer1!.Id}");

        // Assert - Verify deletion was successful
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify customer is marked as deleted when retrieved directly
        var getDeletedResponse = await client.GetAsync($"/customer/v1/customers/{customer1.Id}");
        if (getDeletedResponse.StatusCode == HttpStatusCode.OK)
        {
            var deletedCustomer = await getDeletedResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            Assert.True(deletedCustomer!.IsDeleted);
        }

        // Verify deleted customer is excluded from list queries
        var listResponse = await client.GetAsync("/customer/v1/customers");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listResult = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<CustomerResponse>>();
        Assert.NotNull(listResult);
        Assert.DoesNotContain(listResult!.Items, c => c.Id == customer1.Id);
        Assert.Contains(listResult.Items, c => c.Id == customer2!.Id);

        // Verify historical data is preserved in database
        using var dbContext = _factory.GetDbContext();
        var customerInDb = await dbContext.Customers
            .IgnoreQueryFilters() // Include soft-deleted entities
            .FirstOrDefaultAsync(c => c.Id == customer1.Id);

        Assert.NotNull(customerInDb);
        Assert.True(customerInDb!.IsDeleted);
        Assert.Equal("Retail", customerInDb.Segment);
        Assert.Equal("en", customerInDb.PreferredLanguage);
    }

    /// <summary>
    /// Scenario 9: Create customer with IAM integration enabled → verify PrincipalId is set
    /// </summary>
    [Fact]
    public async Task Scenario9_CreateCustomer_WithIAMEnabled_ReturnsCustomerWithPrincipalId()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();

        var principalId = Guid.NewGuid();
        _factory.MockIAMClient
            .Setup(x => x.CreatePrincipalAsync(It.IsAny<CreatePrincipalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePrincipalResponse { PrincipalId = principalId, CreatedAt = DateTime.UtcNow });

        // Enable feature flag via WithWebHostBuilder and custom configuration
        using var factoryWithIAM = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:PrincipalBasedAuthEnabled"] = "true"
                });
            });
        });

        var client = factoryWithIAM.CreateClient();
        // Manually authenticate since we can't use helper from factoryWithIAM easily if it's not castable
        var token = _factory.CreateTestJwtToken("test-employee", EmployeeRoles);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var email = UniqueEmail("iam.user");
        var request = new
        {
            firstName = "IAM",
            lastName = "User",
            email,
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "UTC"
        };

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/customers", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(customer);
        Assert.Equal(principalId, customer!.PrincipalId);

        // Verify in DB
        using var dbContext = _factory.GetDbContext();
        var customerInDb = await dbContext.Customers.FindAsync(customer.Id);
        Assert.NotNull(customerInDb);
        Assert.Equal(principalId, customerInDb!.PrincipalId);
    }
}
