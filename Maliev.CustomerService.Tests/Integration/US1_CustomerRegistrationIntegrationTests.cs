using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

/// <summary>
/// Integration tests for User Story 1 - Customer Registration and Basic Information Management
/// Tests all 8 acceptance scenarios using real HTTP requests
/// </summary>
[Collection("Database Collection")]
public class US1_CustomerRegistrationIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public US1_CustomerRegistrationIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Scenario 1: Create customer with valid data → verify returned data
    /// </summary>
    [Fact]
    public async Task Scenario1_CreateCustomer_WithValidData_ReturnsCreatedCustomer()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();
        var request = new
        {
            firstName = "John",
            lastName = "Doe",
            email = "john.doe@example.com",
            phone = "+66-2-123-4567",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/customers", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.Id.Should().NotBeEmpty();
        customer.FirstName.Should().Be("John");
        customer.LastName.Should().Be("Doe");
        customer.Email.Should().Be("john.doe@example.com");
        customer.Phone.Should().Be("+66-2-123-4567");
        customer.Segment.Should().Be("Retail");
        customer.Tier.Should().Be("Bronze");
        customer.PreferredLanguage.Should().Be("en");
        customer.Timezone.Should().Be("Asia/Bangkok");
        customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        customer.IsDeleted.Should().BeFalse();
    }

    /// <summary>
    /// Scenario 2: Create customer with duplicate email → verify validation error
    /// </summary>
    [Fact]
    public async Task Scenario2_CreateCustomer_WithDuplicateEmail_ReturnsConflictError()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();
        var request1 = new
        {
            firstName = "Jane",
            lastName = "Smith",
            email = "jane.smith@example.com",
            phone = "+66-2-123-4567",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var request2 = new
        {
            firstName = "John",
            lastName = "Doe",
            email = "jane.smith@example.com", // Duplicate email
            phone = "+66-2-999-9999",
            segment = "Wholesale",
            tier = "Silver",
            preferredLanguage = "th",
            timezone = "Asia/Bangkok"
        };

        // Act
        var response1 = await client.PostAsJsonAsync("/v1/customers", request1);
        var response2 = await client.PostAsJsonAsync("/v1/customers", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var error = await response2.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Code.Should().Be("DUPLICATE_EMAIL");
        error.Message.Should().Contain("already exists");
    }

    /// <summary>
    /// Scenario 3: Retrieve customer by ID → verify complete data
    /// </summary>
    [Fact]
    public async Task Scenario3_RetrieveCustomer_ById_ReturnsCompleteCustomerData()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();
        var createRequest = new
        {
            firstName = "Alice",
            lastName = "Johnson",
            email = "alice.johnson@example.com",
            phone = "+66-2-555-5555",
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

        var createResponse = await client.PostAsJsonAsync("/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Act
        var getResponse = await client.GetAsync($"/v1/customers/{createdCustomer!.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var customer = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.Id.Should().Be(createdCustomer.Id);
        customer.FirstName.Should().Be("Alice");
        customer.LastName.Should().Be("Johnson");
        customer.Email.Should().Be("alice.johnson@example.com");
        customer.Phone.Should().Be("+66-2-555-5555");
        customer.Segment.Should().Be("Enterprise");
        customer.Tier.Should().Be("Gold");
        customer.PreferredLanguage.Should().Be("th");
        customer.Timezone.Should().Be("Asia/Bangkok");
        customer.CommunicationPreferences.Should().NotBeNull();
        customer.CommunicationPreferences.Should().ContainKey("email_opt_in");
        customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        customer.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Scenario 4: Update customer (employee actor) → verify changes and audit log
    /// </summary>
    [Fact]
    public async Task Scenario4_UpdateCustomer_ByEmployee_RecordsEmployeeActorInAudit()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();
        var createRequest = new
        {
            firstName = "Bob",
            lastName = "Wilson",
            email = "bob.wilson@example.com",
            phone = "+66-2-111-1111",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var createResponse = await client.PostAsJsonAsync("/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var updateRequest = new
        {
            phone = "+66-2-222-2222",
            lastName = "Wilson-Updated"
        };

        // Act
        var updateResponse = await client.PatchAsJsonAsync($"/v1/customers/{createdCustomer!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.Phone.Should().Be("+66-2-222-2222");
        updatedCustomer.LastName.Should().Be("Wilson-Updated");
        updatedCustomer.FirstName.Should().Be("Bob"); // Unchanged
        updatedCustomer.UpdatedAt.Should().BeAfter(updatedCustomer.CreatedAt);

        // Verify audit log entry exists for employee actor
        using var dbContext = _factory.GetDbContext();
        var auditLogs = await dbContext.AuditLogs
            .Where(a => a.EntityId == createdCustomer.Id.ToString() && a.Action == "Update")
            .ToListAsync();

        auditLogs.Should().NotBeEmpty();
        auditLogs.Should().Contain(a => a.ActorType == "Employee");
    }

    /// <summary>
    /// Scenario 5: Update customer (customer self-service) → verify actorType = "Customer"
    /// </summary>
    [Fact]
    public async Task Scenario5_UpdateCustomer_BySelf_RecordsCustomerActorInAudit()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var employeeClient = _factory.CreateEmployeeClient();
        var createRequest = new
        {
            firstName = "Charlie",
            lastName = "Brown",
            email = "charlie.brown@example.com",
            phone = "+66-2-333-3333",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var createResponse = await employeeClient.PostAsJsonAsync("/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create customer client with the customer's ID
        var customerClient = _factory.CreateCustomerClient(createdCustomer!.Id.ToString());
        var updateRequest = new
        {
            phone = "+66-2-444-4444"
        };

        // Act
        var updateResponse = await customerClient.PatchAsJsonAsync($"/v1/customers/{createdCustomer.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.Phone.Should().Be("+66-2-444-4444");

        // Verify audit log entry exists for customer actor
        using var dbContext = _factory.GetDbContext();
        var auditLogs = await dbContext.AuditLogs
            .Where(a => a.EntityId == createdCustomer.Id.ToString() && a.Action == "Update")
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        auditLogs.Should().NotBeEmpty();
        auditLogs.First().ActorType.Should().Be("Customer");
    }

    /// <summary>
    /// Scenario 6: Update preferred_language and timezone → verify changes
    /// </summary>
    [Fact]
    public async Task Scenario6_UpdateCustomer_PreferredLanguageAndTimezone_ChangesApplied()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();
        var createRequest = new
        {
            firstName = "Diana",
            lastName = "Prince",
            email = "diana.prince@example.com",
            phone = "+66-2-777-7777",
            segment = "Wholesale",
            tier = "Silver",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var createResponse = await client.PostAsJsonAsync("/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var updateRequest = new
        {
            preferredLanguage = "th",
            timezone = "Asia/Singapore"
        };

        // Act
        var updateResponse = await client.PatchAsJsonAsync($"/v1/customers/{createdCustomer!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.PreferredLanguage.Should().Be("th");
        updatedCustomer.Timezone.Should().Be("Asia/Singapore");
        updatedCustomer.UpdatedAt.Should().BeAfter(updatedCustomer.CreatedAt);

        // Verify changes in audit log
        using var dbContext = _factory.GetDbContext();
        var auditLog = await dbContext.AuditLogs
            .Where(a => a.EntityId == createdCustomer.Id.ToString() && a.Action == "Update")
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        auditLog.Should().NotBeNull();
    }

    /// <summary>
    /// Scenario 7: Update communication_preferences → verify JSONB storage
    /// </summary>
    [Fact]
    public async Task Scenario7_UpdateCustomer_CommunicationPreferences_StoredAsJsonb()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();
        var createRequest = new
        {
            firstName = "Edward",
            lastName = "Norton",
            email = "edward.norton@example.com",
            phone = "+66-2-888-8888",
            segment = "Government",
            tier = "Platinum",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var createResponse = await client.PostAsJsonAsync("/v1/customers", createRequest);
        var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var updateRequest = new
        {
            communicationPreferences = new Dictionary<string, object>
            {
                { "email_opt_in", true },
                { "sms_opt_in", false },
                { "marketing_opt_in", true }
            }
        };

        // Act
        var updateResponse = await client.PatchAsJsonAsync($"/v1/customers/{createdCustomer!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedCustomer = await updateResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        updatedCustomer.Should().NotBeNull();
        updatedCustomer!.CommunicationPreferences.Should().NotBeNull();
        updatedCustomer.CommunicationPreferences.Should().ContainKey("email_opt_in");
        updatedCustomer.CommunicationPreferences.Should().ContainKey("sms_opt_in");
        updatedCustomer.CommunicationPreferences.Should().ContainKey("marketing_opt_in");
        updatedCustomer.CommunicationPreferences!["email_opt_in"].ToString().Should().Be("True");
        updatedCustomer.CommunicationPreferences["sms_opt_in"].ToString().Should().Be("False");
        updatedCustomer.CommunicationPreferences["marketing_opt_in"].ToString().Should().Be("True");

        // Verify the data is persisted in the database
        var getResponse = await client.GetAsync($"/v1/customers/{createdCustomer.Id}");
        var retrievedCustomer = await getResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        retrievedCustomer!.CommunicationPreferences.Should().NotBeNull();
        retrievedCustomer.CommunicationPreferences.Should().HaveCount(3);
    }

    /// <summary>
    /// Scenario 8: Soft delete customer → verify isDeleted flag and exclusion from queries
    /// </summary>
    [Fact]
    public async Task Scenario8_SoftDeleteCustomer_MarkedAsDeletedAndExcludedFromQueries()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();

        // Create two customers
        var request1 = new
        {
            firstName = "Frank",
            lastName = "Castle",
            email = "frank.castle@example.com",
            phone = "+66-2-111-1111",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var request2 = new
        {
            firstName = "Grace",
            lastName = "Hopper",
            email = "grace.hopper@example.com",
            phone = "+66-2-222-2222",
            segment = "Enterprise",
            tier = "Gold",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };

        var response1 = await client.PostAsJsonAsync("/v1/customers", request1);
        var customer1 = await response1.Content.ReadFromJsonAsync<CustomerResponse>();

        var response2 = await client.PostAsJsonAsync("/v1/customers", request2);
        var customer2 = await response2.Content.ReadFromJsonAsync<CustomerResponse>();

        // Act - Soft delete the first customer
        var deleteResponse = await client.DeleteAsync($"/v1/customers/{customer1!.Id}");

        // Assert - Verify deletion was successful
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify customer is marked as deleted when retrieved directly
        var getDeletedResponse = await client.GetAsync($"/v1/customers/{customer1.Id}");
        if (getDeletedResponse.StatusCode == HttpStatusCode.OK)
        {
            var deletedCustomer = await getDeletedResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            deletedCustomer!.IsDeleted.Should().BeTrue();
        }

        // Verify deleted customer is excluded from list queries
        var listResponse = await client.GetAsync("/v1/customers");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listResult = await listResponse.Content.ReadFromJsonAsync<PaginatedResponse<CustomerResponse>>();
        listResult.Should().NotBeNull();
        listResult!.Items.Should().NotContain(c => c.Id == customer1.Id);
        listResult.Items.Should().Contain(c => c.Id == customer2!.Id);

        // Verify historical data is preserved in database
        using var dbContext = _factory.GetDbContext();
        var customerInDb = await dbContext.Customers
            .IgnoreQueryFilters() // Include soft-deleted entities
            .FirstOrDefaultAsync(c => c.Id == customer1.Id);

        customerInDb.Should().NotBeNull();
        customerInDb!.IsDeleted.Should().BeTrue();
        customerInDb.Segment.Should().Be("Retail");
        customerInDb.PreferredLanguage.Should().Be("en");
    }
}
