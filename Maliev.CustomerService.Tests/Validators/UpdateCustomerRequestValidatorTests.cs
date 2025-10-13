using FluentAssertions;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Validators;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Tests.Validators;

public class UpdateCustomerRequestValidatorTests
{
    private readonly UpdateCustomerRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            FirstName = "Jane",
            Email = "jane.doe@example.com"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingVersion_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            FirstName = "Jane"
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
        var request = new UpdateCustomerRequest
        {
            Version = Array.Empty<byte>(),
            FirstName = "Jane"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Version");
    }

    [Fact]
    public async Task Validate_WithExcessiveFirstNameLength_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            FirstName = new string('A', 101)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName" && e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public async Task Validate_WithExcessiveLastNameLength_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            LastName = new string('B', 101)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName" && e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            Email = "invalid-email"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("valid email"));
    }

    [Fact]
    public async Task Validate_WithInvalidPhoneFormat_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            Phone = "123-456-7890" // Invalid E.164 format
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Phone" && e.ErrorMessage.Contains("E.164"));
    }

    [Fact]
    public async Task Validate_WithValidE164Phone_ReturnsValid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            Phone = "+66812345678"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithInvalidSegment_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            Segment = "InvalidSegment"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Segment");
    }

    [Fact]
    public async Task Validate_WithInvalidTier_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            Tier = "InvalidTier"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tier");
    }

    [Fact]
    public async Task Validate_WithInvalidPreferredLanguageLength_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            PreferredLanguage = "ENG" // Should be 2 characters
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PreferredLanguage");
    }

    [Fact]
    public async Task Validate_WithUppercaseLanguageCode_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            PreferredLanguage = "EN" // Should be lowercase
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PreferredLanguage" && e.ErrorMessage.Contains("ISO 639-1"));
    }

    [Fact]
    public async Task Validate_WithNullOptionalFields_ReturnsValid()
    {
        // Arrange - Only Version is required, all other fields are optional for partial updates
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyGuidCompanyId_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            CompanyId = Guid.Empty
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyId");
    }

    [Fact]
    public async Task Validate_WithValidCompanyId_ReturnsValid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            CompanyId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidTimezone_ReturnsValid()
    {
        // Arrange
        var request = new UpdateCustomerRequest
        {
            Version = new byte[] { 1, 2, 3, 4 },
            Timezone = "Asia/Bangkok"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
