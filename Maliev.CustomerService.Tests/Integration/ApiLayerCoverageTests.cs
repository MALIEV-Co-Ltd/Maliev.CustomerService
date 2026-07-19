using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Api.Models;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class ApiLayerCoverageTests
{
    private readonly TestWebApplicationFactory _factory;

    public ApiLayerCoverageTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region CustomerController - Full CRUD Coverage

    [Fact]
    public async Task Customer_GetById_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.customers.read" });

        var customerId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Customers.Add(new Customer { Id = customerId, FirstName = "A", LastName = "B", Email = "a@b.com", Segment = "R", Tier = "B" });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/customers/{customerId}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Customer_Update_ReturnsConflict()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.customers.update" });

        var customerId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Customers.Add(new Customer { Id = customerId, FirstName = "A", LastName = "B", Email = "a@b.com", Segment = "R", Tier = "B" });
        await db.SaveChangesAsync();

        var res = await client.PatchAsJsonAsync($"/customer/v1/customers/{customerId}", new Api.Models.Customers.UpdateCustomerRequest { FirstName = "C", xmin = 1 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Customer_Delete_ReturnsNoContent()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.customers.delete" });

        var customerId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Customers.Add(new Customer { Id = customerId, FirstName = "A", LastName = "B", Email = "a@b.com", Segment = "R", Tier = "B" });
        await db.SaveChangesAsync();

        // Refresh to get the actual xmin value
        var customer = await db.Customers.FindAsync(customerId);
        var customerXmin = db.Entry(customer!).Property<uint>("xmin").CurrentValue;
        var request = new Api.Models.Customers.DeleteCustomerRequest { xmin = customerXmin };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/customers/{customerId}")
        {
            Content = JsonContent.Create(request)
        };
        var res = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task Customer_CheckEmail_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.customers.read" });

        var res = await client.GetAsync("/customer/v1/customers/check-email?email=test@test.com");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Customer_History_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.customers.read" });

        var customerId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Customers.Add(new Customer { Id = customerId, FirstName = "A", LastName = "B", Email = "a@b.com", Segment = "R", Tier = "B" });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/customers/{customerId}/history");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Customer_Preferences_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.customers.list" });

        var res = await client.GetAsync("/customer/v1/customers/preferences");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    #endregion

    #region CompanyController - Full CRUD Coverage

    [Fact]
    public async Task Company_Create_ReturnsCreated()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.companies.manage" });

        var res = await client.PostAsJsonAsync("/customer/v1/companies", new Api.Models.Companies.CreateCompanyRequest { Name = "Test", Segment = "R" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Company_GetById_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.companies.read" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Companies.Add(new Company { Id = id, Name = "Test", Segment = "R" });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/companies/{id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Company_GetAll_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.companies.read" });

        var res = await client.GetAsync("/customer/v1/companies");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Company_Search_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.companies.read" });

        var res = await client.GetAsync("/customer/v1/companies/search?query=test");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Company_Update_ReturnsNotFound()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.companies.manage" });

        var res = await client.PatchAsJsonAsync($"/customer/v1/companies/{Guid.NewGuid()}", new Api.Models.Companies.UpdateCompanyRequest { Name = "Updated" });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Company_GetWithCustomers_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.companies.read" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Companies.Add(new Company { Id = id, Name = "Test", Segment = "R" });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/companies/{id}/customers");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Company_CalculateTier_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.tiers.manage" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Companies.Add(new Company { Id = id, Name = "Test", Segment = "R" });
        await db.SaveChangesAsync();

        var res = await client.PostAsync($"/customer/v1/companies/{id}/calculate-tier", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    #endregion

    #region AddressController - Full Coverage

    [Fact]
    public async Task Address_Create_ReturnsCreated()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.addresses.manage" });

        var res = await client.PostAsJsonAsync("/customer/v1/addresses", new Api.Models.Addresses.CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "123 St",
            City = "BKK",
            StateProvince = "BKK",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Address_GetByOwner_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.addresses.manage" });

        var res = await client.GetAsync($"/customer/v1/addresses?ownerType=Customer&ownerId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Address_Update_ReturnsConflict()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.addresses.manage" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Addresses.Add(new Address { Id = id, OwnerType = OwnerType.Customer, OwnerId = Guid.NewGuid(), Type = AddressType.Billing, AddressLine1 = "123", City = "BKK", StateProvince = "BKK", PostalCode = "10110", CountryId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        var res = await client.PatchAsJsonAsync($"/customer/v1/addresses/{id}", new Api.Models.Addresses.UpdateAddressRequest { AddressLine1 = "456", xmin = 1 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Address_Delete_ReturnsNoContent()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.addresses.manage" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Addresses.Add(new Address { Id = id, OwnerType = OwnerType.Customer, OwnerId = Guid.NewGuid(), Type = AddressType.Billing, AddressLine1 = "123", City = "BKK", StateProvince = "BKK", PostalCode = "10110", CountryId = Guid.NewGuid() });
        await db.SaveChangesAsync();

        // Refresh to get the actual xmin value
        var address = await db.Addresses.FindAsync(id);
        var addressXmin = db.Entry(address!).Property<uint>("xmin").CurrentValue;
        var request = new Api.Models.Addresses.DeleteAddressRequest { xmin = addressXmin };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/addresses/{id}")
        {
            Content = JsonContent.Create(request)
        };
        var res = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    #endregion

    #region DocumentController - Full Coverage

    [Fact]
    public async Task Document_Create_ReturnsCreated()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.documents.create" });

        var res = await client.PostAsJsonAsync("/customer/v1/documents", new Api.Models.Documents.CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "NDA",
            FileReference = "f1",
            Filename = "d.pdf"
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Document_GetByOwner_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.documents.read" });

        var res = await client.GetAsync($"/customer/v1/documents?ownerType=Customer&ownerId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Document_Update_ReturnsBadRequest()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.documents.create" });

        var res = await client.PatchAsJsonAsync($"/customer/v1/documents/{Guid.NewGuid()}", new Api.Models.Documents.UpdateDocumentRequest { FileReference = "f2" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Document_Complete_ReturnsNotFound()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.documents.create" });

        var res = await client.PatchAsync($"/customer/v1/documents/{Guid.NewGuid()}/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Document_Delete_ReturnsNotFound()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.documents.delete" });

        var request = new Api.Models.Documents.DeleteDocumentRequest { xmin = 0 };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/documents/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(request)
        };
        var res = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    #endregion

    #region InternalNoteController - Full Coverage

    [Fact]
    public async Task InternalNote_Create_ReturnsCreated()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.notes.create" });

        var res = await client.PostAsJsonAsync("/customer/v1/internal-notes", new Api.Models.InternalNotes.CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = "Test note"
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task InternalNote_GetByOwner_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.notes.read" });

        var res = await client.GetAsync($"/customer/v1/internal-notes?ownerType=Customer&ownerId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task InternalNote_Update_ReturnsConflict()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.notes.update" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.InternalNotes.Add(new InternalNote { Id = id, OwnerType = "Customer", OwnerId = Guid.NewGuid(), NoteText = "Test", CreatedBy = "user" });
        await db.SaveChangesAsync();

        var res = await client.PatchAsJsonAsync($"/customer/v1/internal-notes/{id}", new Api.Models.InternalNotes.UpdateInternalNoteRequest { NoteText = "Updated", xmin = 1 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task InternalNote_Delete_ReturnsNoContent()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.notes.delete" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.InternalNotes.Add(new InternalNote { Id = id, OwnerType = "Customer", OwnerId = Guid.NewGuid(), NoteText = "Test", CreatedBy = "user" });
        await db.SaveChangesAsync();

        // Refresh to get the actual xmin value
        var note = await db.InternalNotes.FindAsync(id);
        var noteXmin = db.Entry(note!).Property<uint>("xmin").CurrentValue;
        var request = new Api.Models.InternalNotes.DeleteInternalNoteRequest { xmin = noteXmin };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/internal-notes/{id}")
        {
            Content = JsonContent.Create(request)
        };
        var res = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    [Fact]
    public async Task InternalNote_AddComment_ReturnsCreated()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.notes.update" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.InternalNotes.Add(new InternalNote { Id = id, OwnerType = "Customer", OwnerId = Guid.NewGuid(), NoteText = "Test", CreatedBy = "user" });
        await db.SaveChangesAsync();

        var res = await client.PostAsJsonAsync($"/customer/v1/internal-notes/{id}/comments", new Api.Models.InternalNotes.CreateInternalNoteCommentRequest { CommentText = "Comment" });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task InternalNote_GetComments_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.notes.read" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.InternalNotes.Add(new InternalNote { Id = id, OwnerType = "Customer", OwnerId = Guid.NewGuid(), NoteText = "Test", CreatedBy = "user" });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/internal-notes/{id}/comments");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task InternalNote_GetActivity_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.notes.read" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.InternalNotes.Add(new InternalNote { Id = id, OwnerType = "Customer", OwnerId = Guid.NewGuid(), NoteText = "Test", CreatedBy = "user" });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/internal-notes/{id}/activity");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    #endregion

    #region NDAController - Full Coverage

    [Fact]
    public async Task NDA_Create_ReturnsCreated()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.ndas.create" });

        var res = await client.PostAsJsonAsync("/customer/v1/ndas", new Api.Models.NDAs.CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task NDA_GetById_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.ndas.read" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.NDARecords.Add(new NDARecord { Id = id, CustomerId = Guid.NewGuid(), Status = NDAStatus.Draft, ExpiresAt = DateTime.UtcNow.AddYears(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/ndas/{id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task NDA_GetByCustomerId_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.ndas.read" });

        var cid = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.NDARecords.Add(new NDARecord { Id = Guid.NewGuid(), CustomerId = cid, Status = NDAStatus.Draft, ExpiresAt = DateTime.UtcNow.AddYears(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/ndas/customer/{cid}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task NDA_UpdateStatus_ReturnsConflict()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.ndas.update" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.NDARecords.Add(new NDARecord { Id = id, CustomerId = Guid.NewGuid(), Status = NDAStatus.Draft, ExpiresAt = DateTime.UtcNow.AddYears(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var res = await client.PatchAsJsonAsync($"/customer/v1/ndas/{id}/status", new Api.Models.NDAs.UpdateNDAStatusRequest { Status = NDAStatus.Signed, xmin = 1 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task NDA_GetHistory_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.ndas.read" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.NDARecords.Add(new NDARecord { Id = id, CustomerId = Guid.NewGuid(), Status = NDAStatus.Draft, ExpiresAt = DateTime.UtcNow.AddYears(1), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/ndas/{id}/history");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    #endregion

    #region TierSettingsController - Full Coverage

    [Fact]
    public async Task TierSettings_GetAll_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.tiers.read" });

        var res = await client.GetAsync("/customer/v1/tier-settings");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task TierSettings_Create_ReturnsCreated()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.tiers.manage" });

        var res = await client.PostAsJsonAsync("/customer/v1/tier-settings", new { tierName = "Gold", minPurchaseValue = 100000, minOrderCount = 10 });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task TierSettings_GetById_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.tiers.read" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.CompanyTierSettings.Add(new CompanyTierSettings { Id = id, TierName = "Gold", MinPurchaseValue = 100000, MinOrderCount = 10, ValidFrom = DateTime.UtcNow.AddYears(-1), ValidTo = DateTime.UtcNow.AddYears(1) });
        await db.SaveChangesAsync();

        var res = await client.GetAsync($"/customer/v1/tier-settings/{id}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task TierSettings_Update_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient("test-user", new[] { "Admin" }, new[] { "customer.tiers.manage" });

        var id = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.CompanyTierSettings.Add(new CompanyTierSettings { Id = id, TierName = "Gold", MinPurchaseValue = 100000, MinOrderCount = 10, ValidFrom = DateTime.UtcNow.AddYears(-1), ValidTo = DateTime.UtcNow.AddYears(1) });
        await db.SaveChangesAsync();

        var res = await client.PutAsJsonAsync($"/customer/v1/tier-settings/{id}", new { tierName = "Platinum", minPurchaseValue = 200000, minOrderCount = 20 });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    #endregion
}
