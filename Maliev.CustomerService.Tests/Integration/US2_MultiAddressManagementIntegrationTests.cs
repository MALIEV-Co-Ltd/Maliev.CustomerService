using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

/// <summary>
/// Integration tests for User Story 2 - Multi-Address Management for Billing and Shipping
/// Tests all 5 acceptance scenarios using real HTTP requests
/// </summary>
[Collection("Database Collection")]
public class US2_MultiAddressManagementIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public US2_MultiAddressManagementIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Scenario 1: Add billing address with country validation
    /// </summary>
    [Fact]
    public async Task Scenario1_AddBillingAddress_WithCountryValidation_CreatesAddress()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();

        // Mock Country Service to validate country ID
        var mockCountryId = Guid.NewGuid();
        _factory.MockCountryService
            .Setup(x => x.ValidateCountryIdAsync(mockCountryId))
            .ReturnsAsync(true);

        // Create a customer first
        var customerRequest = new
        {
            firstName = "John",
            lastName = "Smith",
            email = "john.smith@example.com",
            phone = "+66-2-123-4567",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create billing address
        var addressRequest = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Billing",
            addressLine1 = "123 Main Street",
            addressLine2 = "Suite 100",
            city = "Bangkok",
            province = "Bangkok",
            postalCode = "10110",
            countryId = mockCountryId
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/addresses", addressRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var address = await response.Content.ReadFromJsonAsync<AddressResponse>();
        address.Should().NotBeNull();
        address!.Id.Should().NotBeEmpty();
        address.OwnerType.Should().Be("Customer");
        address.OwnerId.Should().Be(customer.Id);
        address.Type.Should().Be("Billing");
        address.AddressLine1.Should().Be("123 Main Street");
        address.AddressLine2.Should().Be("Suite 100");
        address.City.Should().Be("Bangkok");
        address.Province.Should().Be("Bangkok");
        address.PostalCode.Should().Be("10110");
        address.CountryId.Should().Be(mockCountryId);
        address.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify Country Service was called
        _factory.MockCountryService.Verify(
            x => x.ValidateCountryIdAsync(mockCountryId),
            Times.Once);
    }

    /// <summary>
    /// Scenario 2: Add second shipping address for same customer
    /// </summary>
    [Fact]
    public async Task Scenario2_AddSecondShippingAddress_ForSameCustomer_CreatesMultipleShippingAddresses()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();

        var mockCountryId1 = Guid.NewGuid();
        var mockCountryId2 = Guid.NewGuid();
        _factory.MockCountryService
            .Setup(x => x.ValidateCountryIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        // Create a customer
        var customerRequest = new
        {
            firstName = "Jane",
            lastName = "Doe",
            email = "jane.doe@example.com",
            phone = "+66-2-555-5555",
            segment = "Wholesale",
            tier = "Silver",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create first shipping address
        var address1Request = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Shipping",
            addressLine1 = "456 Warehouse Road",
            city = "Bangkok",
            province = "Bangkok",
            postalCode = "10220",
            countryId = mockCountryId1
        };

        // Create second shipping address
        var address2Request = new
        {
            ownerType = "Customer",
            ownerId = customer.Id,
            type = "Shipping",
            addressLine1 = "789 Distribution Center",
            city = "Chiang Mai",
            province = "Chiang Mai",
            postalCode = "50000",
            countryId = mockCountryId2
        };

        // Act
        var response1 = await client.PostAsJsonAsync("/v1/addresses", address1Request);
        var response2 = await client.PostAsJsonAsync("/v1/addresses", address2Request);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        var address1 = await response1.Content.ReadFromJsonAsync<AddressResponse>();
        var address2 = await response2.Content.ReadFromJsonAsync<AddressResponse>();

        address1.Should().NotBeNull();
        address2.Should().NotBeNull();
        address1!.Type.Should().Be("Shipping");
        address2!.Type.Should().Be("Shipping");
        address1.OwnerId.Should().Be(customer.Id);
        address2.OwnerId.Should().Be(customer.Id);
        address1.AddressLine1.Should().Be("456 Warehouse Road");
        address2.AddressLine1.Should().Be("789 Distribution Center");

        // Verify both addresses exist for the same customer
        var getResponse = await client.GetAsync($"/v1/addresses/Customer/{customer.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var addresses = await getResponse.Content.ReadFromJsonAsync<List<AddressResponse>>();
        addresses.Should().NotBeNull();
        addresses!.Should().HaveCount(2);
        addresses.Should().Contain(a => a.Id == address1.Id);
        addresses.Should().Contain(a => a.Id == address2.Id);
    }

    /// <summary>
    /// Scenario 3: Update address postal code and province
    /// </summary>
    [Fact]
    public async Task Scenario3_UpdateAddress_PostalCodeAndProvince_ChangesApplied()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();

        var mockCountryId = Guid.NewGuid();
        _factory.MockCountryService
            .Setup(x => x.ValidateCountryIdAsync(mockCountryId))
            .ReturnsAsync(true);

        // Create a customer
        var customerRequest = new
        {
            firstName = "Bob",
            lastName = "Johnson",
            email = "bob.johnson@example.com",
            phone = "+66-2-777-7777",
            segment = "Enterprise",
            tier = "Gold",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create address
        var addressRequest = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Billing",
            addressLine1 = "100 Business Plaza",
            city = "Bangkok",
            province = "Bangkok",
            postalCode = "10100",
            countryId = mockCountryId
        };
        var createResponse = await client.PostAsJsonAsync("/v1/addresses", addressRequest);
        var createdAddress = await createResponse.Content.ReadFromJsonAsync<AddressResponse>();

        // Update postal code and province
        var updateRequest = new
        {
            postalCode = "10330",
            province = "Nonthaburi"
        };

        // Act
        var updateResponse = await client.PatchAsJsonAsync($"/v1/addresses/{createdAddress!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedAddress = await updateResponse.Content.ReadFromJsonAsync<AddressResponse>();
        updatedAddress.Should().NotBeNull();
        updatedAddress!.PostalCode.Should().Be("10330");
        updatedAddress.Province.Should().Be("Nonthaburi");
        updatedAddress.AddressLine1.Should().Be("100 Business Plaza"); // Unchanged
        updatedAddress.City.Should().Be("Bangkok"); // Unchanged
        updatedAddress.UpdatedAt.Should().BeAfter(updatedAddress.CreatedAt);
    }

    /// <summary>
    /// Scenario 4: Retrieve all addresses for customer
    /// </summary>
    [Fact]
    public async Task Scenario4_RetrieveAllAddresses_ForCustomer_ReturnsAllAddressesGroupedByType()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();

        var mockCountryId = Guid.NewGuid();
        _factory.MockCountryService
            .Setup(x => x.ValidateCountryIdAsync(mockCountryId))
            .ReturnsAsync(true);

        // Create a customer
        var customerRequest = new
        {
            firstName = "Alice",
            lastName = "Williams",
            email = "alice.williams@example.com",
            phone = "+66-2-888-8888",
            segment = "Government",
            tier = "Platinum",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create billing address
        var billingRequest = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Billing",
            addressLine1 = "200 Government Complex",
            city = "Bangkok",
            province = "Bangkok",
            postalCode = "10400",
            countryId = mockCountryId
        };

        // Create two shipping addresses
        var shipping1Request = new
        {
            ownerType = "Customer",
            ownerId = customer.Id,
            type = "Shipping",
            addressLine1 = "300 Warehouse A",
            city = "Samut Prakan",
            province = "Samut Prakan",
            postalCode = "10540",
            countryId = mockCountryId
        };

        var shipping2Request = new
        {
            ownerType = "Customer",
            ownerId = customer.Id,
            type = "Shipping",
            addressLine1 = "400 Warehouse B",
            city = "Chonburi",
            province = "Chonburi",
            postalCode = "20000",
            countryId = mockCountryId
        };

        await client.PostAsJsonAsync("/v1/addresses", billingRequest);
        await client.PostAsJsonAsync("/v1/addresses", shipping1Request);
        await client.PostAsJsonAsync("/v1/addresses", shipping2Request);

        // Act
        var getResponse = await client.GetAsync($"/v1/addresses/Customer/{customer.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var addresses = await getResponse.Content.ReadFromJsonAsync<List<AddressResponse>>();
        addresses.Should().NotBeNull();
        addresses!.Should().HaveCount(3);

        var billingAddresses = addresses.Where(a => a.Type == "Billing").ToList();
        var shippingAddresses = addresses.Where(a => a.Type == "Shipping").ToList();

        billingAddresses.Should().HaveCount(1);
        shippingAddresses.Should().HaveCount(2);

        billingAddresses.First().AddressLine1.Should().Be("200 Government Complex");
        shippingAddresses.Should().Contain(a => a.AddressLine1 == "300 Warehouse A");
        shippingAddresses.Should().Contain(a => a.AddressLine1 == "400 Warehouse B");
    }

    /// <summary>
    /// Scenario 5: Delete address
    /// </summary>
    [Fact]
    public async Task Scenario5_DeleteAddress_RemovesAddressFromCustomer()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateEmployeeClient();

        var mockCountryId = Guid.NewGuid();
        _factory.MockCountryService
            .Setup(x => x.ValidateCountryIdAsync(mockCountryId))
            .ReturnsAsync(true);

        // Create a customer
        var customerRequest = new
        {
            firstName = "Charlie",
            lastName = "Brown",
            email = "charlie.brown@example.com",
            phone = "+66-2-999-9999",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create two addresses
        var address1Request = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Shipping",
            addressLine1 = "500 Old Address",
            city = "Bangkok",
            province = "Bangkok",
            postalCode = "10500",
            countryId = mockCountryId
        };

        var address2Request = new
        {
            ownerType = "Customer",
            ownerId = customer.Id,
            type = "Shipping",
            addressLine1 = "600 New Address",
            city = "Bangkok",
            province = "Bangkok",
            postalCode = "10600",
            countryId = mockCountryId
        };

        var response1 = await client.PostAsJsonAsync("/v1/addresses", address1Request);
        var response2 = await client.PostAsJsonAsync("/v1/addresses", address2Request);
        var address1 = await response1.Content.ReadFromJsonAsync<AddressResponse>();
        var address2 = await response2.Content.ReadFromJsonAsync<AddressResponse>();

        // Act - Delete the first address
        var deleteResponse = await client.DeleteAsync($"/v1/addresses/{address1!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify address is removed from customer's address list
        var getResponse = await client.GetAsync($"/v1/addresses/Customer/{customer.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var addresses = await getResponse.Content.ReadFromJsonAsync<List<AddressResponse>>();
        addresses.Should().NotBeNull();
        addresses!.Should().HaveCount(1);
        addresses.Should().NotContain(a => a.Id == address1.Id);
        addresses.Should().Contain(a => a.Id == address2!.Id);
        addresses.First().AddressLine1.Should().Be("600 New Address");
    }
}
