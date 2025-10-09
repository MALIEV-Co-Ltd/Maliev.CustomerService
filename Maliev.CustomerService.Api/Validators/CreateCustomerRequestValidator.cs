using FluentValidation;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Data.Models;
using System.Text.RegularExpressions;

namespace Maliev.CustomerService.Api.Validators;

/// <summary>
/// Validator for CreateCustomerRequest using FluentValidation
/// </summary>
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    private static readonly Regex Iso639Regex = new(@"^[a-z]{2}$", RegexOptions.Compiled);
    private static readonly Regex E164PhoneRegex = new(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled);

    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address (RFC 5322)")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Phone)
            .Must(BeValidE164Phone).When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format (e.g., +1234567890)")
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must not exceed 20 characters");

        RuleFor(x => x.Segment)
            .NotEmpty().WithMessage("Segment is required")
            .Must(BeValidSegment).WithMessage($"Segment must be one of: {string.Join(", ", CustomerSegment.All)}");

        RuleFor(x => x.Tier)
            .NotEmpty().WithMessage("Tier is required")
            .Must(BeValidTier).WithMessage($"Tier must be one of: {string.Join(", ", CustomerTier.All)}");

        RuleFor(x => x.PreferredLanguage)
            .NotEmpty().WithMessage("Preferred language is required")
            .Length(2).WithMessage("Preferred language must be exactly 2 characters")
            .Must(BeValidIso639Code).WithMessage("Preferred language must be a valid ISO 639-1 code (lowercase, e.g., 'en', 'th')");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required")
            .MaximumLength(50).WithMessage("Timezone must not exceed 50 characters")
            .Must(BeValidTimezone).WithMessage("Timezone must be a valid IANA timezone identifier (e.g., 'UTC', 'Asia/Bangkok')");

        RuleFor(x => x.CompanyId)
            .Must(id => id == null || id != Guid.Empty).When(x => x.CompanyId.HasValue)
            .WithMessage("Company ID must be a valid GUID when provided");
    }

    private bool BeValidE164Phone(string? phone)
    {
        if (string.IsNullOrEmpty(phone))
            return true;

        return E164PhoneRegex.IsMatch(phone);
    }

    private bool BeValidSegment(string segment)
    {
        return CustomerSegment.All.Contains(segment);
    }

    private bool BeValidTier(string tier)
    {
        return CustomerTier.All.Contains(tier);
    }

    private bool BeValidIso639Code(string language)
    {
        return Iso639Regex.IsMatch(language);
    }

    private bool BeValidTimezone(string timezone)
    {
        try
        {
            // Validate against .NET's TimeZoneInfo
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch
        {
            // If not found in system timezones, check if it's "UTC" or matches IANA pattern
            return timezone == "UTC" || timezone.Contains('/');
        }
    }
}
