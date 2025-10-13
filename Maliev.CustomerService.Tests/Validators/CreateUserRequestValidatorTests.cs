using FluentAssertions;
using Maliev.CustomerService.Api.Models.Users;
using Maliev.CustomerService.Api.Validators;

namespace Maliev.CustomerService.Tests.Validators;

public class CreateUserRequestValidatorTests
{
    private readonly CreateUserRequestValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidData_ReturnsValid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            Roles = new List<string> { "User" }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_WithMissingUsername_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "",
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithInvalidUsernameFormat_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john@doe", // Invalid character '@'
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && e.ErrorMessage.Contains("3-50 characters"));
    }

    [Fact]
    public async Task Validate_WithUsernameTooShort_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "ab", // Only 2 characters
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Validate_WithUsernameTooLong_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = new string('a', 51), // 51 characters
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public async Task Validate_WithValidUsernameFormats_ReturnsValid()
    {
        var validUsernames = new[] { "john_doe", "john-doe", "JohnDoe123", "user_123-test" };

        foreach (var username in validUsernames)
        {
            // Arrange
            var request = new CreateUserRequest
            {
                Username = username,
                Email = "john.doe@example.com",
                Password = "SecurePass123!",
                Roles = new List<string>()
            };

            // Act
            var result = await _validator.ValidateAsync(request);

            // Assert
            result.IsValid.Should().BeTrue($"Username '{username}' should be valid");
        }
    }

    [Fact]
    public async Task Validate_WithInvalidEmail_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "invalid-email",
            Password = "SecurePass123!",
            Roles = new List<string>()
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
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = new string('a', 250) + "@example.com",
            Password = "SecurePass123!",
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_WithMissingPassword_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "",
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithPasswordTooShort_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "Pass1!", // Only 6 characters
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("at least 8 characters"));
    }

    [Fact]
    public async Task Validate_WithPasswordMissingUppercase_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "securepass123!", // No uppercase
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("uppercase"));
    }

    [Fact]
    public async Task Validate_WithPasswordMissingLowercase_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "SECUREPASS123!", // No lowercase
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("lowercase"));
    }

    [Fact]
    public async Task Validate_WithPasswordMissingDigit_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "SecurePass!", // No digit
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("digit"));
    }

    [Fact]
    public async Task Validate_WithPasswordMissingSpecialCharacter_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "SecurePass123", // No special character
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("non-alphanumeric"));
    }

    [Fact]
    public async Task Validate_WithNullRoles_ReturnsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            Roles = null!
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Roles" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task Validate_WithEmptyRolesList_ReturnsValid()
    {
        // Arrange - Empty roles list is valid (can be empty)
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            Roles = new List<string>()
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithMultipleRoles_ReturnsValid()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "john_doe",
            Email = "john.doe@example.com",
            Password = "SecurePass123!",
            Roles = new List<string> { "User", "Admin", "Manager" }
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
