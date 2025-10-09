using FluentAssertions;
using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Api.Validators;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Tests.Validators;

public class CreateDocumentRequestValidatorTests
{
    private readonly CreateDocumentRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            DocumentType = "Invoice",
            FileReference = "https://storage.example.com/documents/invoice-001.pdf",
            Filename = "invoice-001.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingOwnerType_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = "",
            OwnerId = Guid.NewGuid(),
            DocumentType = "Invoice",
            FileReference = "https://storage.example.com/documents/invoice-001.pdf",
            Filename = "invoice-001.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerType" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithInvalidOwnerType_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = "InvalidType",
            OwnerId = Guid.NewGuid(),
            DocumentType = "Invoice",
            FileReference = "https://storage.example.com/documents/invoice-001.pdf",
            Filename = "invoice-001.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerType");
    }

    [Fact]
    public async Task Validate_WithMissingOwnerId_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.Empty,
            DocumentType = "Invoice",
            FileReference = "https://storage.example.com/documents/invoice-001.pdf",
            Filename = "invoice-001.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerId");
    }

    [Fact]
    public async Task Validate_WithMissingDocumentType_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            DocumentType = "",
            FileReference = "https://storage.example.com/documents/invoice-001.pdf",
            Filename = "invoice-001.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentType" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveDocumentTypeLength_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            DocumentType = new string('A', 101),
            FileReference = "https://storage.example.com/documents/invoice-001.pdf",
            Filename = "invoice-001.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DocumentType" && e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public async Task Validate_WithMissingFileReference_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            DocumentType = "Invoice",
            FileReference = "",
            Filename = "invoice-001.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileReference" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveFileReferenceLength_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            DocumentType = "Invoice",
            FileReference = new string('A', 501),
            Filename = "invoice-001.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileReference" && e.ErrorMessage.Contains("500 characters"));
    }

    [Fact]
    public async Task Validate_WithMissingFilename_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            DocumentType = "Invoice",
            FileReference = "https://storage.example.com/documents/invoice-001.pdf",
            Filename = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Filename" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveFilenameLength_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateDocumentRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            DocumentType = "Invoice",
            FileReference = "https://storage.example.com/documents/invoice-001.pdf",
            Filename = new string('A', 256)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Filename" && e.ErrorMessage.Contains("255 characters"));
    }

    [Fact]
    public async Task Validate_WithAllValidOwnerTypes_ReturnsValid()
    {
        var ownerTypes = new[] { OwnerType.Customer, OwnerType.Company };

        foreach (var ownerType in ownerTypes)
        {
            // Arrange
            var request = new CreateDocumentRequest
            {
                OwnerType = ownerType,
                OwnerId = Guid.NewGuid(),
                DocumentType = "Invoice",
                FileReference = "https://storage.example.com/documents/invoice-001.pdf",
                Filename = "invoice-001.pdf"
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"OwnerType '{ownerType}' should be valid");
        }
    }

    [Fact]
    public async Task Validate_WithVariousDocumentTypes_ReturnsValid()
    {
        var documentTypes = new[] { "Invoice", "Receipt", "Contract", "NDA", "Quote" };

        foreach (var documentType in documentTypes)
        {
            // Arrange
            var request = new CreateDocumentRequest
            {
                OwnerType = OwnerType.Customer,
                OwnerId = Guid.NewGuid(),
                DocumentType = documentType,
                FileReference = "https://storage.example.com/documents/file.pdf",
                Filename = "file.pdf"
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"DocumentType '{documentType}' should be valid");
        }
    }
}

public class UpdateDocumentRequestValidatorTests
{
    private readonly UpdateDocumentRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new UpdateDocumentRequest
        {
            FileReference = "https://storage.example.com/documents/updated-invoice.pdf",
            Filename = "updated-invoice.pdf",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingFileReference_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateDocumentRequest
        {
            FileReference = "",
            Filename = "updated-invoice.pdf",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileReference" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveFileReferenceLength_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateDocumentRequest
        {
            FileReference = new string('A', 501),
            Filename = "updated-invoice.pdf",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FileReference");
    }

    [Fact]
    public async Task Validate_WithMissingFilename_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateDocumentRequest
        {
            FileReference = "https://storage.example.com/documents/updated-invoice.pdf",
            Filename = "",
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Filename" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveFilenameLength_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateDocumentRequest
        {
            FileReference = "https://storage.example.com/documents/updated-invoice.pdf",
            Filename = new string('A', 256),
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Filename");
    }

    [Fact]
    public async Task Validate_WithMissingRowVersion_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateDocumentRequest
        {
            FileReference = "https://storage.example.com/documents/updated-invoice.pdf",
            Filename = "updated-invoice.pdf"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RowVersion" && e.ErrorMessage.Contains("required"));
    }
}
