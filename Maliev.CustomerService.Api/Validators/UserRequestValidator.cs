using FluentValidation;
using Maliev.CustomerService.Api.Models.Users;
using System.Text.RegularExpressions;

namespace Maliev.CustomerService.Api.Validators;

/// <summary>
/// Validator for CreateUserRequest
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_-]{3,50}$", RegexOptions.Compiled);
    private static readonly Regex PasswordComplexityRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        RegexOptions.Compiled);

    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Must(BeValidUsername).WithMessage("Username must be 3-50 characters and contain only letters, numbers, underscores, or hyphens");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address (RFC 5322)")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .Must(MeetPasswordComplexity).WithMessage("Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one non-alphanumeric character");

        RuleFor(x => x.Roles)
            .NotNull().WithMessage("Roles list is required (can be empty)");
    }

    private bool BeValidUsername(string username)
    {
        return UsernameRegex.IsMatch(username);
    }

    private bool MeetPasswordComplexity(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        // Check for at least one uppercase letter
        if (!password.Any(char.IsUpper))
            return false;

        // Check for at least one lowercase letter
        if (!password.Any(char.IsLower))
            return false;

        // Check for at least one digit
        if (!password.Any(char.IsDigit))
            return false;

        // Check for at least one non-alphanumeric character
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return false;

        return true;
    }
}

/// <summary>
/// Validator for UpdatePasswordRequest
/// </summary>
public class UpdatePasswordRequestValidator : AbstractValidator<UpdatePasswordRequest>
{
    public UpdatePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .Must(MeetPasswordComplexity).WithMessage("New password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one non-alphanumeric character");
    }

    private bool MeetPasswordComplexity(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            return false;

        if (!password.Any(char.IsUpper))
            return false;

        if (!password.Any(char.IsLower))
            return false;

        if (!password.Any(char.IsDigit))
            return false;

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return false;

        return true;
    }
}

/// <summary>
/// Validator for UpdateRolesRequest
/// </summary>
public class UpdateRolesRequestValidator : AbstractValidator<UpdateRolesRequest>
{
    public UpdateRolesRequestValidator()
    {
        RuleFor(x => x.Roles)
            .NotNull().WithMessage("Roles list is required (can be empty)");
    }
}

/// <summary>
/// Validator for ValidateCredentialsRequest
/// </summary>
public class ValidateCredentialsRequestValidator : AbstractValidator<ValidateCredentialsRequest>
{
    public ValidateCredentialsRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
