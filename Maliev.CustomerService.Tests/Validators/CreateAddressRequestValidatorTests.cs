using FluentAssertions;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Api.Validators;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Tests.Validators;

public class CreateAddressRequestValidatorTests
{
    private readonly CreateAddressRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
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
        var request = new CreateAddressRequest
        {
            OwnerType = "",
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
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
        var request = new CreateAddressRequest
        {
            OwnerType = "InvalidType",
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
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
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.Empty,
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerId");
    }

    [Fact]
    public async Task Validate_WithInvalidAddressType_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = "InvalidType",
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task Validate_WithMissingAddressLine1_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressLine1" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithExcessiveAddressLine1Length_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = new string('A', 256),
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressLine1");
    }

    [Fact]
    public async Task Validate_WithExcessiveAddressLine2Length_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            AddressLine2 = new string('B', 256),
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressLine2");
    }

    [Fact]
    public async Task Validate_WithMissingCity_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            City = "",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithMissingProvince_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            Province = "",
            PostalCode = "10110",
            CountryId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Province" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithMissingPostalCode_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "",
            CountryId = Guid.NewGuid()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithMissingCountryId_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateAddressRequest
        {
            OwnerType = OwnerType.Customer,
            OwnerId = Guid.NewGuid(),
            Type = AddressType.Billing,
            AddressLine1 = "123 Main St",
            City = "Bangkok",
            Province = "Bangkok",
            PostalCode = "10110",
            CountryId = Guid.Empty
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CountryId");
    }

    [Fact]
    public async Task Validate_WithAllValidOwnerTypes_ReturnsValid()
    {
        var ownerTypes = new[] { OwnerType.Customer, OwnerType.Company };

        foreach (var ownerType in ownerTypes)
        {
            // Arrange
            var request = new CreateAddressRequest
            {
                OwnerType = ownerType,
                OwnerId = Guid.NewGuid(),
                Type = AddressType.Billing,
                AddressLine1 = "123 Main St",
                City = "Bangkok",
                Province = "Bangkok",
                PostalCode = "10110",
                CountryId = Guid.NewGuid()
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"OwnerType '{ownerType}' should be valid");
        }
    }

    [Fact]
    public async Task Validate_WithAllValidAddressTypes_ReturnsValid()
    {
        var addressTypes = new[] { AddressType.Billing, AddressType.Shipping };

        foreach (var addressType in addressTypes)
        {
            // Arrange
            var request = new CreateAddressRequest
            {
                OwnerType = OwnerType.Customer,
                OwnerId = Guid.NewGuid(),
                Type = addressType,
                AddressLine1 = "123 Main St",
                City = "Bangkok",
                Province = "Bangkok",
                PostalCode = "10110",
                CountryId = Guid.NewGuid()
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"AddressType '{addressType}' should be valid");
        }
    }
}
