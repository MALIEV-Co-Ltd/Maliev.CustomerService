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
public class AdditionalControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdditionalControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region CustomerController Tests

    [Fact]
    public async Task Create_WithInvalidEmail_ReturnsBadRequest()
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
            Email = "not-an-email",
            Segment = "Retail",
            Tier = "Bronze"
        };

        var response = await client.PostAsJsonAsync("/customer/v1/customers", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsCustomer_WhenExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var customerId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.Customers.Add(new Customer
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Segment = "Retail",
            Tier = "Bronze"
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/customer/v1/customers/{customerId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(result);
        Assert.Equal(customerId, result.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var response = await client.GetAsync($"/customer/v1/customers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.update" });

        var request = new UpdateCustomerRequest
        {
            FirstName = "Updated",
            xmin = 1
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/customers/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenInvalidVersion()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.update" });

        var customerId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.Customers.Add(new Customer
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Segment = "Retail",
            Tier = "Bronze",
            xmin = 1
        });
        await dbContext.SaveChangesAsync();

        var request = new UpdateCustomerRequest
        {
            FirstName = "Updated",
            xmin = 2
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/customers/{customerId}", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.delete" });

        var request = new Api.Models.Customers.DeleteCustomerRequest { xmin = 0 };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/customers/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(request)
        };
        var response = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CheckEmail_ReturnsTrue_WhenExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var email = "exists@example.com";
        using var dbContext = _factory.GetDbContext();
        dbContext.Customers.Add(new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Segment = "Retail",
            Tier = "Bronze"
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/customer/v1/customers/check-email?email={email}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EmailExistsResponse>();
        Assert.NotNull(result);
        Assert.True(result.Exists);
    }

    [Fact]
    public async Task CheckEmail_ReturnsBadRequest_WhenNoEmailProvided()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var response = await client.GetAsync("/customer/v1/customers/check-email");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_ReturnsActivity()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var customerId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.Customers.Add(new Customer
        {
            Id = customerId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Segment = "Retail",
            Tier = "Bronze"
        });
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = "Customer",
            EntityId = customerId.ToString(),
            Action = AuditAction.Create,
            ActorId = "test-actor",
            ActorType = "Employee",
            Timestamp = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var response = await client.GetAsync($"/customer/v1/customers/{customerId}/history");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPreferences_ReturnsPreferences()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.list" });

        var response = await client.GetAsync("/customer/v1/customers/preferences?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsWithFilters()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.list" });

        var response = await client.GetAsync(
            "/customer/v1/customers?query=test&segment=Retail&tier=Bronze&preferredLanguage=en&email=test@example.com&includeDeleted=true&page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region CompanyController Tests

    [Fact]
    public async Task CreateCompany_ReturnsValidationError()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.manage" });

        var request = new Api.Models.Companies.CreateCompanyRequest
        {
            Name = "",
            Segment = "Retail"
        };

        var response = await client.PostAsJsonAsync("/customer/v1/companies", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithQuery_ReturnsResults()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.read" });

        var response = await client.GetAsync("/customer/v1/companies/search?query=ABC");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CalculateTier_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.manage" });

        var response = await client.PostAsync($"/customer/v1/companies/{Guid.NewGuid()}/calculate-tier", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AppliesMaxPageSize()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.companies.read" });

        var response = await client.GetAsync("/customer/v1/companies?pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region AddressController Tests

    [Fact]
    public async Task CreateAddress_ReturnsValidationError()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.addresses.manage" });

        var request = new Api.Models.Addresses.CreateAddressRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            Type = "Billing",
            AddressLine1 = "",
            City = "BKK",
            StateProvince = "BKK",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync("/customer/v1/addresses", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAddress_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.addresses.manage" });

        var request = new Api.Models.Addresses.UpdateAddressRequest
        {
            AddressLine1 = "123 St",
            xmin = 1
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/addresses/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAddress_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.addresses.manage" });

        var request = new Api.Models.Addresses.DeleteAddressRequest { xmin = 0 };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/addresses/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(request)
        };
        var response = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAddresses_ByOwner_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.addresses.manage" });

        var response = await client.GetAsync($"/customer/v1/addresses?ownerType=Customer&ownerId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region DocumentController Tests

    [Fact]
    public async Task CreateDocument_ReturnsValidationError()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.documents.create" });

        var request = new Api.Models.Documents.CreateDocumentRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            DocumentType = "",
            FileReference = "file-123"
        };

        var response = await client.PostAsJsonAsync("/customer/v1/documents", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDocument_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.documents.create" });

        var request = new Api.Models.Documents.UpdateDocumentRequest
        {
            FileReference = "new-file",
            Filename = "new.pdf",
            xmin = 1
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/documents/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteDocument_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.documents.create" });

        var response = await client.PatchAsync($"/customer/v1/documents/{Guid.NewGuid()}/complete", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteDocument_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.documents.delete" });

        var request = new Api.Models.Documents.DeleteDocumentRequest { xmin = 0 };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/documents/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(request)
        };
        var response = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region InternalNoteController Tests

    [Fact]
    public async Task CreateInternalNote_ReturnsValidationError()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.create" });

        var request = new Api.Models.InternalNotes.CreateInternalNoteRequest
        {
            OwnerType = "Customer",
            OwnerId = Guid.NewGuid(),
            NoteText = ""
        };

        var response = await client.PostAsJsonAsync("/customer/v1/internal-notes", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateInternalNote_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.update" });

        var request = new Api.Models.InternalNotes.UpdateInternalNoteRequest
        {
            NoteText = "Updated text",
            xmin = 1
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/internal-notes/{Guid.NewGuid()}", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteInternalNote_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.delete" });

        var request = new Api.Models.InternalNotes.DeleteInternalNoteRequest { xmin = 0 };
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customer/v1/internal-notes/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(request)
        };
        var response = await client.SendAsync(httpRequest);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_ReturnsNotFound_WhenNoteNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.update" });

        var request = new Api.Models.InternalNotes.CreateInternalNoteCommentRequest
        {
            CommentText = "Test comment"
        };

        var response = await client.PostAsJsonAsync($"/customer/v1/internal-notes/{Guid.NewGuid()}/comments", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_ReturnsEmpty_WhenNoteNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.read" });

        var response = await client.GetAsync($"/customer/v1/internal-notes/{Guid.NewGuid()}/comments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetActivity_ReturnsEmpty_WhenNoteNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.notes.read" });

        var response = await client.GetAsync($"/customer/v1/internal-notes/{Guid.NewGuid()}/activity");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region NDA Controller Tests

    [Fact]
    public async Task UpdateNDAStatus_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.update" });

        var request = new Api.Models.NDAs.UpdateNDAStatusRequest
        {
            Status = Domain.Entities.NDAStatus.Signed,
            SignedBy = "Tester",
            SignedAt = DateTime.UtcNow,
            xmin = 1
        };

        var response = await client.PatchAsJsonAsync($"/customer/v1/ndas/{Guid.NewGuid()}/status", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNDA_ReturnsNotFound_WhenNotExists()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.ndas.delete" });

        var request = new Api.Models.NDAs.DeleteNDARequest
        {
            xmin = 1
        };

        var response = await client.PostAsJsonAsync($"/customer/v1/ndas/{Guid.NewGuid()}/delete", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_ReturnsOk_Always()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.customers.read" });

        var response = await client.GetAsync($"/customer/v1/customers/{Guid.NewGuid()}/history");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region TierSettings Controller Tests

    [Fact]
    public async Task CreateTierSettings_WithInvalidData_ReturnsOk()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.manage" });

        var request = new
        {
            tierName = "",
            minPurchaseValue = -100
        };

        var response = await client.PostAsJsonAsync("/customer/v1/tier-settings", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTierSettings_ReturnsUpdated()
    {
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { "customer.tiers.manage" });

        var tierId = Guid.NewGuid();
        using var dbContext = _factory.GetDbContext();
        dbContext.CompanyTierSettings.Add(new CompanyTierSettings
        {
            Id = tierId,
            TierName = "Gold",
            MinPurchaseValue = 100000,
            MinOrderCount = 10,
            ValidFrom = DateTime.UtcNow.AddYears(-1),
            ValidTo = DateTime.UtcNow.AddYears(1)
        });
        await dbContext.SaveChangesAsync();

        var request = new
        {
            tierName = "Platinum",
            minPurchaseValue = 500000,
            minOrderCount = 50,
            discountPercentage = 15,
            freeShippingMinOrder = 10000,
            coinRewardPercentage = 10,
            validFrom = DateTime.UtcNow,
            validTo = DateTime.UtcNow.AddYears(1)
        };

        var response = await client.PutAsJsonAsync($"/customer/v1/tier-settings/{tierId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion
}
