using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Domain.Authorization;
using Maliev.CustomerService.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

/// <summary>
/// Integration tests for User Story 2 - Multi-Address Management for Billing and Shipping
/// Tests all 5 acceptance scenarios using real HTTP requests
/// </summary>
[Collection("Database Collection")]
public class US2_MultiAddressManagementIntegrationTests
{
    private readonly TestWebApplicationFactory _factory;
    private readonly string _testId;

    private static readonly string[] EmployeeRoles = { "roles.customer.representative" };
    private static readonly string[] EmployeePermissions =
    {
        CustomerPermissions.CustomersCreate,
        CustomerPermissions.CustomersRead,
        CustomerPermissions.CustomersUpdate,
        CustomerPermissions.CustomersList,
        CustomerPermissions.AddressesManage
    };

    public US2_MultiAddressManagementIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _testId = Guid.NewGuid().ToString("N")[..8];
    }


    private string UniqueEmail(string prefix) => $"{prefix}.{_testId}@example.com";

    /// <summary>
    /// Scenario 1: Add billing address with country validation
    /// </summary>
    [Fact]
    public async Task Scenario1_AddBillingAddress_WithCountryValidation_CreatesAddress()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles, EmployeePermissions);

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
            email = UniqueEmail("john.smith"),
            phone = "+6621234567",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/customer/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create billing address
        var addressRequest = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Billing",
            addressLine1 = "123 Main Street",
            addressLine2 = "Suite 100",
            addressLine3 = "Floor 10",
            district = "Pathum Wan",
            city = "Bangkok",
            stateProvince = "Bangkok",
            postalCode = "10110",
            countryId = mockCountryId
        };

        // Act
        var response = await client.PostAsJsonAsync("/customer/v1/addresses", addressRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var address = await response.Content.ReadFromJsonAsync<AddressResponse>();
        Assert.NotNull(address);
        Assert.NotEqual(Guid.Empty, address!.Id);
        Assert.Equal("Customer", address.OwnerType);
        Assert.Equal(customer.Id, address.OwnerId);
        Assert.Equal("Billing", address.Type);
        Assert.Equal("123 Main Street", address.AddressLine1);
        Assert.Equal("Suite 100", address.AddressLine2);
        Assert.Equal("Floor 10", address.AddressLine3);
        Assert.Equal("Pathum Wan", address.District);
        Assert.Equal("Bangkok", address.City);
        Assert.Equal("Bangkok", address.StateProvince);
        Assert.Equal("10110", address.PostalCode);
        Assert.Equal(mockCountryId, address.CountryId);
        Assert.True(Math.Abs((address.CreatedAt - DateTime.UtcNow).TotalSeconds) < 5);

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
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles, EmployeePermissions);

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
            email = UniqueEmail("jane.doe"),
            phone = "+6625555555",
            segment = "Wholesale",
            tier = "Silver",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/customer/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create first shipping address
        var address1Request = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Shipping",
            addressLine1 = "456 Warehouse Road",
            city = "Bangkok",
            stateProvince = "Bangkok",
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
            stateProvince = "Chiang Mai",
            postalCode = "50000",
            countryId = mockCountryId2
        };

        // Act
        var response1 = await client.PostAsJsonAsync("/customer/v1/addresses", address1Request);
        var response2 = await client.PostAsJsonAsync("/customer/v1/addresses", address2Request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        var address1 = await response1.Content.ReadFromJsonAsync<AddressResponse>();
        var address2 = await response2.Content.ReadFromJsonAsync<AddressResponse>();

        Assert.NotNull(address1);
        Assert.NotNull(address2);
        Assert.Equal("Shipping", address1!.Type);
        Assert.Equal("Shipping", address2!.Type);
        Assert.Equal(customer.Id, address1.OwnerId);
        Assert.Equal(customer.Id, address2.OwnerId);
        Assert.Equal("456 Warehouse Road", address1.AddressLine1);
        Assert.Equal("789 Distribution Center", address2.AddressLine1);

        // Verify both addresses exist for the same customer
        var getResponse = await client.GetAsync($"/customer/v1/addresses?ownerType=Customer&ownerId={customer.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var addresses = await getResponse.Content.ReadFromJsonAsync<List<AddressResponse>>();
        Assert.NotNull(addresses);
        Assert.Equal(2, addresses!.Count);
        Assert.Contains(addresses, a => a.Id == address1.Id);
        Assert.Contains(addresses, a => a.Id == address2.Id);
    }

    /// <summary>
    /// Scenario 3: Update address postal code and province
    /// </summary>
    [Fact]
    public async Task Scenario3_UpdateAddress_PostalCodeAndProvince_ChangesApplied()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles, EmployeePermissions);

        var mockCountryId = Guid.NewGuid();
        _factory.MockCountryService
            .Setup(x => x.ValidateCountryIdAsync(mockCountryId))
            .ReturnsAsync(true);

        // Create a customer
        var customerRequest = new
        {
            firstName = "Bob",
            lastName = "Johnson",
            email = UniqueEmail("bob.johnson"),
            phone = "+6627777777",
            segment = "Enterprise",
            tier = "Gold",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/customer/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create address
        var addressRequest = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Billing",
            addressLine1 = "100 Business Plaza",
            city = "Bangkok",
            stateProvince = "Bangkok",
            postalCode = "10100",
            countryId = mockCountryId
        };
        var createResponse = await client.PostAsJsonAsync("/customer/v1/addresses", addressRequest);
        var createdAddress = await createResponse.Content.ReadFromJsonAsync<AddressResponse>();

        // Update postal code and province
        var updateRequest = new
        {
            postalCode = "10330",
            stateProvince = "Nonthaburi",
            xmin = createdAddress!.xmin
        };

        // Act
        var updateResponse = await client.PatchAsJsonAsync($"/customer/v1/addresses/{createdAddress.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedAddress = await updateResponse.Content.ReadFromJsonAsync<AddressResponse>();
        Assert.NotNull(updatedAddress);
        Assert.Equal("10330", updatedAddress!.PostalCode);
        Assert.Equal("Nonthaburi", updatedAddress.StateProvince);
        Assert.Equal("100 Business Plaza", updatedAddress.AddressLine1); // Unchanged
        Assert.Equal("Bangkok", updatedAddress.City); // Unchanged
        Assert.True(updatedAddress.UpdatedAt > updatedAddress.CreatedAt);
    }

    /// <summary>
    /// Scenario 4: Retrieve all addresses for customer
    /// </summary>
    [Fact]
    public async Task Scenario4_RetrieveAllAddresses_ForCustomer_ReturnsAllAddressesGroupedByType()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles, EmployeePermissions);

        var mockCountryId = Guid.NewGuid();
        _factory.MockCountryService
            .Setup(x => x.ValidateCountryIdAsync(mockCountryId))
            .ReturnsAsync(true);

        // Create a customer
        var customerRequest = new
        {
            firstName = "Alice",
            lastName = "Williams",
            email = UniqueEmail("alice.williams"),
            phone = "+6628888888",
            segment = "Government",
            tier = "Platinum",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/customer/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create billing address
        var billingRequest = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Billing",
            addressLine1 = "200 Government Complex",
            city = "Bangkok",
            stateProvince = "Bangkok",
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
            stateProvince = "Samut Prakan",
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
            stateProvince = "Chonburi",
            postalCode = "20000",
            countryId = mockCountryId
        };

        await client.PostAsJsonAsync("/customer/v1/addresses", billingRequest);
        await client.PostAsJsonAsync("/customer/v1/addresses", shipping1Request);
        await client.PostAsJsonAsync("/customer/v1/addresses", shipping2Request);

        // Act
        var getResponse = await client.GetAsync($"/customer/v1/addresses?ownerType=Customer&ownerId={customer.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var addresses = await getResponse.Content.ReadFromJsonAsync<List<AddressResponse>>();
        Assert.NotNull(addresses);
        Assert.Equal(3, addresses!.Count);

        var billingAddresses = addresses.Where(a => a.Type == "Billing").ToList();
        var shippingAddresses = addresses.Where(a => a.Type == "Shipping").ToList();

        Assert.Single(billingAddresses);
        Assert.Equal(2, shippingAddresses.Count);

        Assert.Equal("200 Government Complex", billingAddresses.First().AddressLine1);
        Assert.Contains(shippingAddresses, a => a.AddressLine1 == "300 Warehouse A");
        Assert.Contains(shippingAddresses, a => a.AddressLine1 == "400 Warehouse B");
    }

    /// <summary>
    /// Scenario 5: Delete address
    /// </summary>
    [Fact]
    public async Task Scenario5_DeleteAddress_RemovesAddressFromCustomer()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-employee", EmployeeRoles, EmployeePermissions);

        var mockCountryId = Guid.NewGuid();
        _factory.MockCountryService
            .Setup(x => x.ValidateCountryIdAsync(mockCountryId))
            .ReturnsAsync(true);

        // Create a customer
        var customerRequest = new
        {
            firstName = "Charlie",
            lastName = "Brown",
            email = UniqueEmail("charlie.brown"),
            phone = "+6629999999",
            segment = "Retail",
            tier = "Bronze",
            preferredLanguage = "en",
            timezone = "Asia/Bangkok"
        };
        var customerResponse = await client.PostAsJsonAsync("/customer/v1/customers", customerRequest);
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        // Create two addresses
        var address1Request = new
        {
            ownerType = "Customer",
            ownerId = customer!.Id,
            type = "Shipping",
            addressLine1 = "500 Old Address",
            city = "Bangkok",
            stateProvince = "Bangkok",
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
            stateProvince = "Bangkok",
            postalCode = "10600",
            countryId = mockCountryId
        };

        var response1 = await client.PostAsJsonAsync("/customer/v1/addresses", address1Request);
        var response2 = await client.PostAsJsonAsync("/customer/v1/addresses", address2Request);
        var address1 = await response1.Content.ReadFromJsonAsync<AddressResponse>();
        var address2 = await response2.Content.ReadFromJsonAsync<AddressResponse>();

        // Act - Delete the first address
        var deleteResponse = await client.DeleteAsync($"/customer/v1/addresses/{address1!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify address is removed from customer's address list
        var getResponse = await client.GetAsync($"/customer/v1/addresses?ownerType=Customer&ownerId={customer.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var addresses = await getResponse.Content.ReadFromJsonAsync<List<AddressResponse>>();
        Assert.NotNull(addresses);
        Assert.Single(addresses);
        Assert.DoesNotContain(addresses!, a => a.Id == address1.Id);
        Assert.Contains(addresses!, a => a.Id == address2!.Id);
        Assert.Equal("600 New Address", addresses!.First().AddressLine1);
    }
}
