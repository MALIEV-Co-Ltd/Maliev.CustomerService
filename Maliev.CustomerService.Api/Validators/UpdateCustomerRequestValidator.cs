using FluentValidation;
using Maliev.CustomerService.Api.Models.Customers;
using Maliev.CustomerService.Data.Models;
using System.Text.RegularExpressions;

namespace Maliev.CustomerService.Api.Validators;

/// <summary>
/// Validator for UpdateCustomerRequest using FluentValidation
/// </summary>
public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    private static readonly Regex Iso639Regex = new(@"^[a-z]{2}$", RegexOptions.Compiled);
    private static readonly Regex E164PhoneRegex = new(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled);

    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required for optimistic concurrency control")
            .Must(version => version != null && version.Length > 0)
            .WithMessage("Version must be a valid row version");

        RuleFor(x => x.FirstName)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.FirstName))
            .WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.LastName))
            .WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must be a valid email address (RFC 5322)")
            .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Phone)
            .Must(BeValidE164Phone).When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must be in E.164 format (e.g., +1234567890)")
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Phone must not exceed 20 characters");

        RuleFor(x => x.Segment)
            .Must(BeValidSegment).When(x => !string.IsNullOrEmpty(x.Segment))
            .WithMessage($"Segment must be one of: {string.Join(", ", CustomerSegment.All)}");

        RuleFor(x => x.Tier)
            .Must(BeValidTier).When(x => !string.IsNullOrEmpty(x.Tier))
            .WithMessage($"Tier must be one of: {string.Join(", ", CustomerTier.All)}");

        RuleFor(x => x.PreferredLanguage)
            .Length(2).When(x => !string.IsNullOrEmpty(x.PreferredLanguage))
            .WithMessage("Preferred language must be exactly 2 characters")
            .Must(BeValidIso639Code).When(x => !string.IsNullOrEmpty(x.PreferredLanguage))
            .WithMessage("Preferred language must be a valid ISO 639-1 code (lowercase, e.g., 'en', 'th')");

        RuleFor(x => x.Timezone)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.Timezone))
            .WithMessage("Timezone must not exceed 50 characters")
            .Must(BeValidTimezone).When(x => !string.IsNullOrEmpty(x.Timezone))
            .WithMessage("Timezone must be a valid IANA timezone identifier (e.g., 'UTC', 'Asia/Bangkok')");

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

    private bool BeValidSegment(string? segment)
    {
        if (string.IsNullOrEmpty(segment))
            return true;

        return CustomerSegment.All.Contains(segment);
    }

    private bool BeValidTier(string? tier)
    {
        if (string.IsNullOrEmpty(tier))
            return true;

        return CustomerTier.All.Contains(tier);
    }

    private bool BeValidIso639Code(string? language)
    {
        if (string.IsNullOrEmpty(language))
            return true;

        return Iso639Regex.IsMatch(language);
    }

    private bool BeValidTimezone(string? timezone)
    {
        if (string.IsNullOrEmpty(timezone))
            return true;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch
        {
            return timezone == "UTC" || timezone.Contains('/');
        }
    }
}
