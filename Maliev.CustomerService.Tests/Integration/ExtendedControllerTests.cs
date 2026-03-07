using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class ExtendedControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public ExtendedControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region CustomerController - Additional Tests

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsConflict()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.create" });

        var request = new CreateCustomerRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "duplicate@test.com",
            Segment = "Retail",
            Tier = "Bronze"
        };

        await client.PostAsJsonAsync("/customer/v1/customers", request);
        var response2 = await client.PostAsJsonAsync("/customer/v1/customers", request);
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidEmailDomain_ReturnsBadRequest()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.create" });

        var request = new CreateCustomerRequest
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@",
            Segment = "Retail",
            Tier = "Bronze"
        };

        var response = await client.PostAsJsonAsync("/customer/v1/customers", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithPagination_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.list" });

        var response = await client.GetAsync("/customer/v1/customers?page=2&pageSize=25");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.list" });

        var response = await client.GetAsync("/customer/v1/customers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPreferences_WithPagination_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.list" });

        var response = await client.GetAsync("/customer/v1/customers/preferences?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region CompanyController - Additional Tests

    [Fact]
    public async Task CreateCompany_WithoutVAT_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.manage" });

        var request = new Api.Models.Companies.CreateCompanyRequest
        {
            Name = "Test Company No VAT",
            Segment = "Retail"
        };

        var response = await client.PostAsJsonAsync("/customer/v1/companies", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCompany_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.manage" });

        var createRequest = new Api.Models.Companies.CreateCompanyRequest
        {
            Name = "Test Company",
            Segment = "Retail"
        };
        var createResponse = await client.PostAsJsonAsync("/customer/v1/companies", createRequest);
        var company = await createResponse.Content.ReadFromJsonAsync<Api.Models.Companies.CompanyResponse>();

        var updateRequest = new Api.Models.Companies.UpdateCompanyRequest
        {
            Name = "Updated Company",
            xmin = company!.xmin
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/companies/{company.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithLimit_ReturnsResults()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.read" });

        var response = await client.GetAsync("/customer/v1/companies/search?query=Test&limit=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CalculateTier_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.manage" });

        var companyId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            Segment = "Retail"
        });
        await dbContext.SaveChangesAsync();

        var response = await client.PostAsync($"/customer/v1/companies/{companyId}/calculate-tier", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region AddressController - Additional Tests

    [Fact]
    public async Task CreateAddress_WithValidData_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.addresses.manage" });

        var ownerId = Guid.NewGuid();

        var request = new Api.Models.Addresses.CreateAddressRequest
        {
            OwnerType = "Company",
            OwnerId = ownerId,
            Type = "Shipping",
            AddressLine1 = "123 Main St",
            AddressLine2 = "Suite 100",
            City = "Bangkok",
            StateProvince = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/customer/v1/addresses", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAddress_ReturnsConflict_WhenVersionMismatch()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.addresses.manage" });

        var addressId = Guid.NewGuid();
        var countryId = Guid.NewGuid();

        using var dbContext = _factory.GetDbContext();
        dbContext.Addresses.Add(new Address
        {
            Id = addressId,
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 St",
            City = "BKK",
            StateProvince = "BKK",
            PostalCode = "10110",
            CountryId = countryId
        });
        await dbContext.SaveChangesAsync();

        var request = new Api.Models.Addresses.UpdateAddressRequest
        {
            AddressLine1 = "456 New St",
            xmin = 2
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/addresses/{addressId}", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAddress_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.addresses.manage" });

        var addressId = Guid.NewGuid();
        var countryId = Guid.NewGuid();

        using var dbContext = _factory.GetDbContext();
        dbContext.Addresses.Add(new Address
        {
            Id = addressId,
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 St",
            City = "BKK",
            StateProvince = "BKK",
            PostalCode = "10110",
            CountryId = countryId
        });
        await dbContext.SaveChangesAsync();

        // Refresh to get the actual xmin value
        var address = await dbContext.Addresses.FindAsync(addressId);
        var addressXmin = dbContext.Entry(address!).Property<uint>("xmin").CurrentValue;
        var request = new Api.Models.Addresses.DeleteAddressRequest { xmin = addressXmin };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/addresses/{addressId}")
        {
            Content = JsonContent.Create(request)
        };
        var response = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    #endregion

    #region DocumentController - Additional Tests

    [Fact]
    public async Task CreateDocument_WithAllFields_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.documents.create" });

        var request = new Api.Models.Documents.CreateDocumentRequest
        {
            OwnerType = "Company",
            OwnerId = Guid.NewGuid(),
            DocumentType = "Contract",
            FileReference = "file-ref-123",
            Filename = "contract.pdf"
        };

        var response = await client.PostAsJsonAsync("/customer/v1/documents", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CompleteDocument_WithSignature_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.documents.create" });

        var createRequest = new Api.Models.Documents.CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "file-123",
            Filename = "nda.pdf"
        };
        var createResponse = await client.PostAsJsonAsync("/customer/v1/documents", createRequest);
        var doc = await createResponse.Content.ReadFromJsonAsync<Api.Models.Documents.DocumentResponse>();

        var completeRequest = new Api.Models.Documents.CompleteDocumentRequest
        {
            SignedBy = "John Doe",
            SignedAt = DateTime.UtcNow
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/documents/{doc!.Id}/complete", completeRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region InternalNoteController - Additional Tests

    [Fact]
    public async Task CreateInternalNote_WithAllFields_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.create" });

        var request = new Api.Models.InternalNotes.CreateInternalNoteRequest
        {
            OwnerType = "Company",
            OwnerId = Guid.NewGuid(),
            NoteText = "Important note about the account"
        };

        var response = await client.PostAsJsonAsync("/customer/v1/internal-notes", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateInternalNote_ReturnsConflict_WhenVersionMismatch()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.update" });

        var noteId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.InternalNotes.Add(new InternalNote
        {
            Id = noteId,
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Original text",
            CreatedBy = "test-user"
        });
        await dbContext.SaveChangesAsync();

        var request = new Api.Models.InternalNotes.UpdateInternalNoteRequest
        {
            NoteText = "Updated text",
            xmin = 2
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/internal-notes/{noteId}", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_ToNote_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.update" });

        var noteId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.InternalNotes.Add(new InternalNote
        {
            Id = noteId,
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Test note",
            CreatedBy = "test-user"
        });
        await dbContext.SaveChangesAsync();

        var request = new Api.Models.InternalNotes.CreateInternalNoteCommentRequest
        {
            CommentText = "Test comment"
        };

        var response = await client.PostAsJsonAsync($"/customer/v1/internal-notes/{noteId}/comments", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_ForNote_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.read" });

        var noteId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.InternalNotes.Add(new InternalNote
        {
            Id = noteId,
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Test note",
            CreatedBy = "test-user"
        });
        dbContext.InternalNoteComments.Add(new InternalNoteComment
        {
            Id = Guid.NewGuid(),
            InternalNoteId = noteId,
            CommentText = "Comment 1",
            CreatedBy = "user1"
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/customer/v1/internal-notes/{noteId}/comments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetActivity_ForNote_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.read" });

        var noteId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.InternalNotes.Add(new InternalNote
        {
            Id = noteId,
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Test note",
            CreatedBy = "test-user"
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/customer/v1/internal-notes/{noteId}/activity");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region NDA Controller - Additional Tests

    [Fact]
    public async Task CreateNDA_WithAllFields_Succeeds()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.create" });

        var request = new Api.Models.NDAs.CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        var response = await client.PostAsJsonAsync("/customer/v1/ndas", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetNDA_ByCustomer_ReturnsList()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.read" });

        var customerId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.NDARecords.Add(new NDARecord
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = NDAStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/customer/v1/ndas/customer/{customerId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateNDAStatus_ReturnsConflict_WhenVersionMismatch()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.update" });

        var ndaId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.NDARecords.Add(new NDARecord
        {
            Id = ndaId,
            CustomerId = Guid.NewGuid(),
            DocumentReferenceId = Guid.NewGuid(),
            Status = NDAStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var request = new Api.Models.NDAs.UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            SignedBy = "John Doe",
            SignedAt = DateTime.UtcNow,
            xmin = 2
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/ndas/{ndaId}/status", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetNDAHistory_ReturnsLogs()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.read" });

        var ndaId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.NDARecords.Add(new NDARecord
        {
            Id = ndaId,
            CustomerId = Guid.NewGuid(),
            Status = NDAStatus.Signed,
            ExpiresAt = DateTime.UtcNow.AddYears(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = "NDARecord",
            EntityId = ndaId.ToString(),
            Action = AuditAction.Create,
            ActorId = "test-actor",
            ActorType = "Employee",
            Timestamp = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/customer/v1/ndas/{ndaId}/history");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Permission Tests

    [Fact]
    public async Task CustomerController_ReturnsForbidden_WhenNoPermission()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        var response = await client.GetAsync("/customer/v1/customers");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CompanyController_ReturnsForbidden_WhenNoPermission()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        var response = await client.GetAsync("/customer/v1/companies");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddressController_ReturnsForbidden_WhenNoPermission()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        var response = await client.GetAsync($"/customer/v1/addresses?ownerType=Customer&ownerId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DocumentController_ReturnsForbidden_WhenNoPermission()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        var response = await client.GetAsync($"/customer/v1/documents?ownerType=Customer&ownerId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InternalNoteController_ReturnsForbidden_WhenNoPermission()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        var response = await client.GetAsync($"/customer/v1/internal-notes?ownerType=Customer&ownerId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NDAController_ReturnsForbidden_WhenNoPermission()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        var response = await client.GetAsync($"/customer/v1/ndas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TierSettingsController_ReturnsForbidden_WhenNoPermission()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            permissions: null);

        var response = await client.GetAsync("/customer/v1/tier-settings");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion
}
