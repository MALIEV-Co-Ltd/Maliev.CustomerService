using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Domain.Authorization;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class CustomerMemoryControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly TestWebApplicationFactory _factory;

    public CustomerMemoryControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GET_CustomerMemories_ReturnsOnlyRequestedCustomerMemories_WithCamelCaseWireShape()
    {
        await _factory.ClearDatabaseAsync();
        var customerId = await SeedCustomerAsync("memory-owner@example.com");
        var otherCustomerId = await SeedCustomerAsync("other-memory-owner@example.com");
        var client = _factory.CreateAuthenticatedClient(
            "quoteengine-bff",
            new[] { "roles.quoteengine.service" },
            new[] { CustomerPermissions.MemoriesRead, CustomerPermissions.MemoriesWrite });

        var observeResponse = await client.PostAsJsonAsync(
            $"/customer/v1/customers/{customerId:D}/memories/observe",
            new CustomerMemoryObserveRequest
            {
                MemoryType = "make_studio_preference",
                Key = "preferred_material",
                Value = "Customer prefers PA12 nylon for functional prototypes.",
                Confidence = 0.84m,
                Source = "quote_agent"
            });
        Assert.Equal(HttpStatusCode.OK, observeResponse.StatusCode);

        var otherObserveResponse = await client.PostAsJsonAsync(
            $"/customer/v1/customers/{otherCustomerId:D}/memories/observe",
            new CustomerMemoryObserveRequest
            {
                MemoryType = "make_studio_preference",
                Key = "preferred_material",
                Value = "Other customer prefers brass.",
                Confidence = 0.7m,
                Source = "quote_agent"
            });
        Assert.Equal(HttpStatusCode.OK, otherObserveResponse.StatusCode);

        var response = await client.GetAsync($"/customer/v1/customers/{customerId:D}/memories?query=nylon&limit=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(document.RootElement.TryGetProperty("customerId", out var customerIdJson));
        Assert.Equal(customerId, customerIdJson.GetGuid());
        Assert.True(document.RootElement.TryGetProperty("items", out var itemsJson));
        var item = Assert.Single(itemsJson.EnumerateArray());
        Assert.Equal("make_studio_preference", item.GetProperty("memoryType").GetString());
        Assert.Equal("preferred_material", item.GetProperty("key").GetString());
        Assert.Equal("Customer prefers PA12 nylon for functional prototypes.", item.GetProperty("value").GetString());
        Assert.Equal("quote_agent", item.GetProperty("source").GetString());
        Assert.Equal(1, item.GetProperty("hitCount").GetInt32());
        Assert.DoesNotContain("brass", item.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task POST_CustomerMemoryObserve_UpsertsExistingMemory_AndIncrementsHitCount()
    {
        await _factory.ClearDatabaseAsync();
        var customerId = await SeedCustomerAsync("memory-upsert@example.com");
        var client = _factory.CreateAuthenticatedClient(
            "quoteengine-bff",
            new[] { "roles.quoteengine.service" },
            new[] { CustomerPermissions.MemoriesRead, CustomerPermissions.MemoriesWrite });

        var first = await client.PostAsJsonAsync(
            $"/customer/v1/customers/{customerId:D}/memories/observe",
            new CustomerMemoryObserveRequest
            {
                MemoryType = "make_studio_preference",
                Key = "tolerance",
                Value = "Standard tolerance is acceptable unless the part is a press fit.",
                Confidence = 0.72m,
                Source = "quote_agent"
            });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync(
            $"/customer/v1/customers/{customerId:D}/memories/observe",
            new CustomerMemoryObserveRequest
            {
                MemoryType = "make_studio_preference",
                Key = "tolerance",
                Value = "Customer usually accepts standard tolerance for non-press-fit parts.",
                Confidence = 0.91m,
                Source = "quote_agent"
            });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var memory = await second.Content.ReadFromJsonAsync<CustomerMemoryResponse>(JsonOptions);
        Assert.NotNull(memory);
        Assert.Equal(customerId, memory!.CustomerId);
        Assert.Equal("tolerance", memory.Key);
        Assert.Equal("Customer usually accepts standard tolerance for non-press-fit parts.", memory.Value);
        Assert.Equal(0.91m, memory.Confidence);
        Assert.Equal(2, memory.HitCount);
    }

    [Fact]
    public async Task GET_CustomerMemories_WithoutMemoryReadPermission_ReturnsForbidden()
    {
        await _factory.ClearDatabaseAsync();
        var customerId = await SeedCustomerAsync("memory-forbidden@example.com");
        var client = _factory.CreateAuthenticatedClient(
            "quoteengine-bff",
            new[] { "roles.quoteengine.service" },
            new[] { CustomerPermissions.CustomersRead });

        var response = await client.GetAsync($"/customer/v1/customers/{customerId:D}/memories");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> SeedCustomerAsync(string email)
    {
        await using var context = _factory.CreateDbContext();
        var customer = new Customer
        {
            PrincipalId = Guid.NewGuid(),
            FirstName = "Memory",
            LastName = "Customer",
            Email = email,
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer.Id;
    }
}
