using System.Net;
using System.Net.Http.Json;
using Maliev.CustomerService.Application.DTOs;
using Maliev.CustomerService.Domain.Authorization;
using Maliev.CustomerService.Domain.Entities;
using Maliev.CustomerService.Tests.Infrastructure;
using Xunit;

namespace Maliev.CustomerService.Tests.Integration;

[Collection("Database Collection")]
public class CompanyDocumentsControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public CompanyDocumentsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithValidCompany_ReturnsDocuments()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { CustomerPermissions.CompaniesRead });

        var companyId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            Segment = "Retail",
            Tier = "Bronze"
        });
        db.CompanyDocuments.Add(new CompanyDocument
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DocumentType = "Tax",
            FileName = "tax.pdf",
            FileUrl = "https://example.com/tax.pdf",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/companies/{companyId}/documents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var documents = await response.Content.ReadFromJsonAsync<List<CompanyDocumentResponse>>();
        Assert.NotNull(documents);
        Assert.Single(documents);
        Assert.Equal("tax.pdf", documents[0].FileName);
    }

    [Fact]
    public async Task GetAll_WithNonExistentCompany_ReturnsNotFound()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { CustomerPermissions.CompaniesRead });

        var companyId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/customer/v1/companies/{companyId}/documents");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { CustomerPermissions.CompaniesManage });

        var companyId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            Segment = "Retail",
            Tier = "Bronze"
        });
        await db.SaveChangesAsync();

        var request = new CreateCompanyDocumentRequest
        {
            DocumentType = "Tax",
            FileName = "tax.pdf",
            FileUrl = "https://example.com/tax.pdf"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/customer/v1/companies/{companyId}/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<CompanyDocumentResponse>();
        Assert.NotNull(document);
        Assert.Equal("tax.pdf", document.FileName);
    }

    [Fact]

    public async Task Create_WithNonExistentCompany_ReturnsNotFound()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { CustomerPermissions.CompaniesManage });

        var companyId = Guid.NewGuid();

        var request = new CreateCompanyDocumentRequest
        {
            DocumentType = "Tax",
            FileName = "tax.pdf",
            FileUrl = "https://example.com/tax.pdf"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/customer/v1/companies/{companyId}/documents", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithValidDocument_ReturnsDocument()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { CustomerPermissions.CompaniesRead });

        var companyId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            Segment = "Retail",
            Tier = "Bronze"
        });
        db.CompanyDocuments.Add(new CompanyDocument
        {
            Id = documentId,
            CompanyId = companyId,
            DocumentType = "Tax",
            FileName = "tax.pdf",
            FileUrl = "https://example.com/tax.pdf",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/companies/{companyId}/documents/{documentId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<CompanyDocumentResponse>();
        Assert.NotNull(document);
        Assert.Equal(documentId, document.Id);
    }

    [Fact]
    public async Task GetById_WithNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { CustomerPermissions.CompaniesRead });

        var companyId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            Segment = "Retail",
            Tier = "Bronze"
        });
        await db.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/customer/v1/companies/{companyId}/documents/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistentDocument_ReturnsNotFound()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(
            "test-employee",
            new[] { "roles.customer.representative" },
            new[] { CustomerPermissions.CompaniesManage });

        var companyId = Guid.NewGuid();
        using var db = _factory.GetDbContext();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Company",
            Segment = "Retail",
            Tier = "Bronze"
        });
        await db.SaveChangesAsync();

        // Act - use a high xmin value that won't match any document
        var response = await client.DeleteAsync($"/customer/v1/companies/{companyId}/documents/{Guid.NewGuid()}?xmin=999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
