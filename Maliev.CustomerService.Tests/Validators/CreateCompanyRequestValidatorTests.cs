using FluentAssertions;
using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Api.Validators;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Tests.Validators;

public class CreateCompanyRequestValidatorTests
{
    private readonly CreateCompanyRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            VatNumber = "TH-1234567890",
            ContactEmail = "contact@acme.com",
            ContactPhone = "+66812345678",
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingName_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "",
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveNameLength_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = new string('A', 256),
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithInvalidVatNumberFormat_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            VatNumber = "1234567890", // Missing country code prefix
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VatNumber" && e.ErrorMessage.Contains("Country Code"));
    }

    [Fact]
    public async Task Validate_WithValidVatNumber_ReturnsValid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            VatNumber = "TH-1234567890",
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithNullVatNumber_ReturnsValid()
    {
        // Arrange - VAT number is optional
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            VatNumber = null,
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithInvalidContactEmail_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            ContactEmail = "invalid-email",
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactEmail" && e.ErrorMessage.Contains("valid email"));
    }

    [Fact]
    public async Task Validate_WithExcessiveContactEmailLength_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            ContactEmail = new string('a', 250) + "@example.com",
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactEmail");
    }

    [Fact]
    public async Task Validate_WithInvalidContactPhone_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            ContactPhone = "123-456-7890", // Invalid E.164 format
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ContactPhone" && e.ErrorMessage.Contains("E.164"));
    }

    [Fact]
    public async Task Validate_WithValidContactPhone_ReturnsValid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            ContactPhone = "+66812345678",
            Segment = CustomerSegment.Enterprise,
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithMissingSegment_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            Segment = "",
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Segment" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithInvalidSegment_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            Segment = "InvalidSegment",
            Tier = CustomerTier.Gold
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Segment");
    }

    [Fact]
    public async Task Validate_WithMissingTier_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            Segment = CustomerSegment.Enterprise,
            Tier = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tier" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithInvalidTier_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateCompanyRequest
        {
            Name = "Acme Corporation",
            Segment = CustomerSegment.Enterprise,
            Tier = "InvalidTier"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tier");
    }

    [Fact]
    public async Task Validate_WithAllValidSegments_ReturnsValid()
    {
        var segments = new[] { CustomerSegment.Retail, CustomerSegment.Wholesale, CustomerSegment.Enterprise, CustomerSegment.Government };

        foreach (var segment in segments)
        {
            // Arrange
            var request = new CreateCompanyRequest
            {
                Name = "Acme Corporation",
                Segment = segment,
                Tier = CustomerTier.Gold
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
        var tiers = new[] { CustomerTier.Bronze, CustomerTier.Silver, CustomerTier.Gold, CustomerTier.Platinum, CustomerTier.VIP };

        foreach (var tier in tiers)
        {
            // Arrange
            var request = new CreateCompanyRequest
            {
                Name = "Acme Corporation",
                Segment = CustomerSegment.Enterprise,
                Tier = tier
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"Tier '{tier}' should be valid");
        }
    }

    [Fact]
    public async Task Validate_WithVariousValidVatFormats_ReturnsValid()
    {
        var validVatNumbers = new[]
        {
            "TH-1234567890",
            "US-12345678901234",
            "GB-123456789012345"
        };

        foreach (var vatNumber in validVatNumbers)
        {
            // Arrange
            var request = new CreateCompanyRequest
            {
                Name = "Acme Corporation",
                VatNumber = vatNumber,
                Segment = CustomerSegment.Enterprise,
                Tier = CustomerTier.Gold
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"VAT number '{vatNumber}' should be valid");
        }
    }
}
