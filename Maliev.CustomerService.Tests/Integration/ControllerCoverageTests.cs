using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Moq;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class ControllerCoverageTests
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ControllerCoverageTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateAuthenticatedClient("test-admin", new[] { "Admin" }, Maliev.CustomerService.Domain.Authorization.CustomerPermissions.All);
    }

    [Fact]
    public async Task CompanyController_Crud_Succeeds()
    {
        await _factory.ClearDatabaseAsync();

        // Create
        var createRequest = new CreateCompanyRequest { Name = "Test Co", Segment = "Retail", Tier = "Bronze" };
        var createResponse = await _client.PostAsJsonAsync("/customer/v1/companies", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var company = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        // Get
        var getResponse = await _client.GetAsync($"/customer/v1/companies/{company!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // List
        var listResponse = await _client.GetAsync("/customer/v1/companies");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    }

    [Fact]
    public async Task AddressController_List_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var response = await _client.GetAsync("/customer/v1/addresses?ownerType=Customer&ownerId=" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DocumentController_Crud_Succeeds()
    {
        await _factory.ClearDatabaseAsync();

        // Create
        var createRequest = new CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-123",
            Filename = "test.pdf"
        };
        var createResponse = await _client.PostAsJsonAsync("/customer/v1/documents", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var doc = await createResponse.Content.ReadFromJsonAsync<DocumentResponse>();

        // Get
        var listResponse = await _client.GetAsync($"/customer/v1/documents?ownerType=Customer&ownerId={createRequest.OwnerId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        // Complete
        var completeRequest = new CompleteDocumentRequest { SignedBy = "Tester", SignedAt = DateTime.UtcNow };
        var completeResponse = await _client.PatchAsJsonAsync($"/customer/v1/documents/{doc!.Id}/complete", completeRequest);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
    }

    [Fact]
    public async Task InternalNoteController_Crud_Succeeds()
    {
        await _factory.ClearDatabaseAsync();

        // Create
        var createRequest = new CreateInternalNoteRequest { OwnerType = "Customer", OwnerId = Guid.NewGuid(), NoteText = "Test Note" };
        var createResponse = await _client.PostAsJsonAsync("/customer/v1/internal-notes", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var note = await createResponse.Content.ReadFromJsonAsync<InternalNoteResponse>();

        // List
        var listResponse = await _client.GetAsync($"/customer/v1/internal-notes?ownerType=Customer&ownerId={createRequest.OwnerId}");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        // Delete
        var deleteResponse = await _client.DeleteAsync($"/customer/v1/internal-notes/{note!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CompanyController_Update_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var createRequest = new CreateCompanyRequest { Name = "Test Co", Segment = "Retail", Tier = "Bronze" };
        var createResponse = await _client.PostAsJsonAsync("/customer/v1/companies", createRequest);
        var company = await createResponse.Content.ReadFromJsonAsync<CompanyResponse>();

        var updateRequest = new UpdateCompanyRequest { Name = "Updated Co", xmin = company!.xmin };
        var updateResponse = await _client.PatchAsJsonAsync($"/customer/v1/companies/{company.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getWithCustomers = await _client.GetAsync($"/customer/v1/companies/{company.Id}/customers");
        Assert.Equal(HttpStatusCode.OK, getWithCustomers.StatusCode);
    }

    [Fact]
    public async Task AddressController_Crud_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var createRequest = new CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 St",
            City = "BKK",
            StateProvince = "BKK",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };
        // Mock country service
        _factory.MockCountryService.Setup(x => x.ValidateCountryIdAsync(createRequest.CountryId)).ReturnsAsync(true);

        var createResponse = await _client.PostAsJsonAsync("/customer/v1/addresses", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var address = await createResponse.Content.ReadFromJsonAsync<AddressResponse>();

        var updateRequest = new UpdateAddressRequest { AddressLine1 = "456 St", xmin = address!.xmin };
        var updateResponse = await _client.PatchAsJsonAsync($"/customer/v1/addresses/{address.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/customer/v1/addresses/{address.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task NDAController_Transitions_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var createRequest = new CreateNDARequest { CustomerId = Guid.NewGuid(), DocumentReferenceId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddYears(1) };
        var createResponse = await _client.PostAsJsonAsync("/customer/v1/ndas", createRequest);
        var nda = await createResponse.Content.ReadFromJsonAsync<NDAResponse>();

        var updateRequest = new UpdateNDAStatusRequest { Status = NDAStatus.Signed, SignedBy = "Tester", SignedAt = DateTime.UtcNow, xmin = nda!.xmin };
        var updateResponse = await _client.PatchAsJsonAsync($"/customer/v1/ndas/{nda.Id}/status", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DocumentController_UpdateDelete_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var createRequest = new CreateDocumentRequest { OwnerType = "Customer", OwnerId = Guid.NewGuid(), DocumentType = "NDA", FileReference = "f1", Filename = "n.pdf" };
        var createResponse = await _client.PostAsJsonAsync("/customer/v1/documents", createRequest);
        var doc = await createResponse.Content.ReadFromJsonAsync<DocumentResponse>();

        var updateRequest = new UpdateDocumentRequest { FileReference = "f2", Filename = "n2.pdf", xmin = doc!.xmin };
        var updateResponse = await _client.PatchAsJsonAsync($"/customer/v1/documents/{doc.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/customer/v1/documents/{doc.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CustomerController_UpdateDelete_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var createRequest = new CreateCustomerRequest { FirstName = "T", LastName = "T", Email = "t@t.com", Segment = "Retail", Tier = "Bronze" };
        var createResponse = await _client.PostAsJsonAsync("/customer/v1/customers", createRequest);
        var customer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();

        var updateRequest = new UpdateCustomerRequest { FirstName = "U", xmin = customer!.xmin };
        var updateResponse = await _client.PatchAsJsonAsync($"/customer/v1/customers/{customer.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var getByPrincipal = await _client.GetAsync($"/customer/v1/customers/by-principal/{customer.PrincipalId}");
        Assert.Equal(HttpStatusCode.OK, getByPrincipal.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/customer/v1/customers/{customer.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CustomerController_GetAllFilters_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var response = await _client.GetAsync("/customer/v1/customers?segment=Retail&tier=Bronze&preferredLanguage=en&email=test&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var response2 = await _client.GetAsync("/customer/v1/customers?includeDeleted=true");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }

    [Fact]
    public async Task CompanyController_GetAllFilters_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var response = await _client.GetAsync("/customer/v1/companies?segment=Retail&tier=Bronze&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CustomerController_ErrorPaths_ReturnsBadRequest()
    {
        await _factory.ClearDatabaseAsync();
        // Invalid email
        var request = new CreateCustomerRequest { FirstName = "T", LastName = "T", Email = "invalid", Segment = "Retail", Tier = "Bronze" };
        var response = await _client.PostAsJsonAsync("/customer/v1/customers", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Conflict (duplicate email)
        var validRequest = new CreateCustomerRequest { FirstName = "T", LastName = "T", Email = "test@test.com", Segment = "Retail", Tier = "Bronze" };
        await _client.PostAsJsonAsync("/customer/v1/customers", validRequest);
        var response2 = await _client.PostAsJsonAsync("/customer/v1/customers", validRequest);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task DocumentController_ListFiltered_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var response = await _client.GetAsync("/customer/v1/documents?ownerType=Company&ownerId=" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CompanyController_GetWithCustomers_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var response = await _client.GetAsync($"/customer/v1/companies/{Guid.NewGuid()}/customers");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InternalNoteController_Update_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var createRequest = new CreateInternalNoteRequest { OwnerType = "Customer", OwnerId = Guid.NewGuid(), NoteText = "Test Note" };
        var createResponse = await _client.PostAsJsonAsync("/customer/v1/internal-notes", createRequest);
        var note = await createResponse.Content.ReadFromJsonAsync<InternalNoteResponse>();

        var updateRequest = new UpdateInternalNoteRequest { NoteText = "Updated", xmin = note!.xmin };
        var updateResponse = await _client.PatchAsJsonAsync($"/customer/v1/internal-notes/{note.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
    }

    [Fact]
    public async Task AddressController_Update_ErrorPaths()
    {
        await _factory.ClearDatabaseAsync();
        var response = await _client.PatchAsJsonAsync($"/customer/v1/addresses/{Guid.NewGuid()}", new UpdateAddressRequest { AddressLine1 = "X", xmin = 0 });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
