using FluentAssertions;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Api.Validators;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Tests.Validators;

public class CreateNDARequestValidatorTests
{
    private readonly CreateNDARequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingCustomerId_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateNDARequest
        {
            CustomerId = Guid.Empty,
            ExpiresAt = DateTime.UtcNow.AddYears(1)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithNullExpiresAt_ReturnsValid()
    {
        // Arrange - ExpiresAt is optional
        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = null
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithFutureExpiresAt_ReturnsValid()
    {
        // Arrange
        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithPastExpiresAt_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(-30)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiresAt" && e.ErrorMessage.Contains("future"));
    }

    [Fact]
    public async Task Validate_WithExpiresAtAtCurrentTime_ReturnsInvalid()
    {
        // Arrange - Current time is not in the future
        var request = new CreateNDARequest
        {
            CustomerId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ExpiresAt");
    }
}

public class UpdateNDAStatusRequestValidatorTests
{
    private readonly UpdateNDAStatusRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidStatusTransition_ReturnsValid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Draft,
            Version = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingStatus_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = "",
            Version = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithInvalidStatus_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = "InvalidStatus",
            Version = new byte[] { 1, 2, 3, 4 }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    [Fact]
    public async Task Validate_WithMissingVersion_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Draft
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Version" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithSignedStatusWithoutSignedBy_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            Version = new byte[] { 1, 2, 3, 4 },
            SignedAt = DateTime.UtcNow
            // Missing SignedBy
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SignedBy" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithSignedStatusWithoutSignedAt_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            Version = new byte[] { 1, 2, 3, 4 },
            SignedBy = "John Doe"
            // Missing SignedAt
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SignedAt" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithSignedStatusWithAllRequiredFields_ReturnsValid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Signed,
            Version = new byte[] { 1, 2, 3, 4 },
            SignedBy = "John Doe",
            SignedAt = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithRevokedStatusWithoutRevokedAt_ReturnsInvalid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Revoked,
            Version = new byte[] { 1, 2, 3, 4 }
            // Missing RevokedAt
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RevokedAt" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithRevokedStatusWithRevokedAt_ReturnsValid()
    {
        // Arrange
        var request = new UpdateNDAStatusRequest
        {
            Status = NDAStatus.Revoked,
            Version = new byte[] { 1, 2, 3, 4 },
            RevokedAt = DateTime.UtcNow
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithAllValidStatuses_ReturnsValid()
    {
        var statuses = new[] { NDAStatus.Draft, NDAStatus.Expired };

        foreach (var status in statuses)
        {
            // Arrange
            var request = new UpdateNDAStatusRequest
            {
                Status = status,
                Version = new byte[] { 1, 2, 3, 4 }
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"Status '{status}' should be valid without additional fields");
        }
    }
}
