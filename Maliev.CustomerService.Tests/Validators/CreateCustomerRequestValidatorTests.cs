using FluentAssertions;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Api.Validators;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Tests.Validators;

public class CreateCustomerRequestValidatorTests
{
    private readonly CreateCustomerRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "+66812345678",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingFirstName_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveFirstNameLength_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = new string('A', 101),
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FirstName" && e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public async Task Validate_WithMissingLastName_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LastName" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid-email",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("valid email"));
    }

    [Fact]
    public async Task Validate_WithExcessiveEmailLength_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = new string('a', 250) + "@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("255 characters"));
    }

    [Fact]
    public async Task Validate_WithInvalidPhoneFormat_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "123-456-7890", // Invalid E.164 format
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
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
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "+66812345678",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullPhone_ReturnsValid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = null,
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
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
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = "InvalidSegment",
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC"
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
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = "InvalidTier",
            PreferredLanguage = "en",
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tier");
    }

    [Fact]
    public async Task Validate_WithInvalidPreferredLanguage_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "ENG", // Should be 2 characters
            Timezone = "UTC"
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
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "EN", // Should be lowercase
            Timezone = "UTC"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PreferredLanguage" && e.ErrorMessage.Contains("ISO 639-1"));
    }

    [Fact]
    public async Task Validate_WithValidTimezone_ReturnsValid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "Asia/Bangkok"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidCompanyId_ReturnsValid()
    {
        // Arrange
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC",
            CompanyId = Guid.NewGuid()
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
        var request = new CreateCustomerRequest
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Segment = CustomerSegment.Retail,
            Tier = CustomerTier.Bronze,
            PreferredLanguage = "en",
            Timezone = "UTC",
            CompanyId = Guid.Empty
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CompanyId");
    }

    [Fact]
    public async Task Validate_WithAllValidSegments_ReturnsValid()
    {
        // Test all valid segment values
        var segments = new[] { CustomerSegment.Retail, CustomerSegment.Wholesale, CustomerSegment.Enterprise, CustomerSegment.Government };

        foreach (var segment in segments)
        {
            // Arrange
            var request = new CreateCustomerRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Segment = segment,
                Tier = CustomerTier.Bronze,
                PreferredLanguage = "en",
                Timezone = "UTC"
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"Segment '{segment}' should be valid");
        }
    }

    [Fact]
    public async Task Validate_WithAllValidTiers_ReturnsValid()
    {
        // Test all valid tier values
        var tiers = new[] { CustomerTier.Bronze, CustomerTier.Silver, CustomerTier.Gold, CustomerTier.Platinum, CustomerTier.VIP };

        foreach (var tier in tiers)
        {
            // Arrange
            var request = new CreateCustomerRequest
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Segment = CustomerSegment.Retail,
                Tier = tier,
                PreferredLanguage = "en",
                Timezone = "UTC"
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"Tier '{tier}' should be valid");
        }
    }
}
