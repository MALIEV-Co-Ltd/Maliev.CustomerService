using FluentAssertions;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Api.Validators;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Tests.Validators;

public class CreateInternalNoteRequestValidatorTests
{
    private readonly CreateInternalNoteRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new CreateInternalNoteRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            NoteText = "This is an internal note about the customer."
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
        var request = new CreateInternalNoteRequest
        {
            OwnerType = "",
            OwnerId = Guid.NewGuid(),
            NoteText = "This is an internal note."
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
        var request = new CreateInternalNoteRequest
        {
            OwnerType = "InvalidType",
            OwnerId = Guid.NewGuid(),
            NoteText = "This is an internal note."
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
        var request = new CreateInternalNoteRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.Empty,
            NoteText = "This is an internal note."
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerId");
    }

    [Fact]
    public async Task Validate_WithMissingNoteText_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateInternalNoteRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            NoteText = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NoteText" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveNoteTextLength_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateInternalNoteRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            NoteText = new string('A', 5001)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NoteText" && e.ErrorMessage.Contains("5000 characters"));
    }

    [Fact]
    public async Task Validate_WithMaximumNoteTextLength_ReturnsValid()
    {
        // Arrange
        var request = new CreateInternalNoteRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            NoteText = new string('A', 5000)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAllValidOwnerTypes_ReturnsValid()
    {
        var ownerTypes = new[] { OwnerType.Customer, OwnerType.Company };

        foreach (var ownerType in ownerTypes)
        {
            // Arrange
            var request = new CreateInternalNoteRequest
            {
                OwnerType = ownerType,
                OwnerId = Guid.NewGuid(),
                NoteText = "This is an internal note."
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"OwnerType '{ownerType}' should be valid");
        }
    }
}

public class UpdateInternalNoteRequestValidatorTests
{
    private readonly UpdateInternalNoteRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new UpdateInternalNoteRequest
        {
            NoteText = "Updated internal note content.",
            Version = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingNoteText_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateInternalNoteRequest
        {
            NoteText = "",
            Version = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NoteText" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveNoteTextLength_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateInternalNoteRequest
        {
            NoteText = new string('A', 5001),
            Version = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NoteText" && e.ErrorMessage.Contains("5000 characters"));
    }

    [Fact]
    public async Task Validate_WithMaximumNoteTextLength_ReturnsValid()
    {
        // Arrange
        var request = new UpdateInternalNoteRequest
        {
            NoteText = new string('A', 5000),
            Version = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithMissingVersion_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateInternalNoteRequest
        {
            NoteText = "Updated internal note content."
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Version" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithEmptyVersion_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateInternalNoteRequest
        {
            NoteText = "Updated internal note content.",
            Version = Array.Empty<byte>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Version");
    }
}
