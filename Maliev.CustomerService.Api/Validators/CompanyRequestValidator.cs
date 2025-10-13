using FluentValidation;
using Maliev.CustomerService.Api.Models.Companies;
using Maliev.CustomerService.Data.Models;
using System.Text.RegularExpressions;

namespace Maliev.CustomerService.Api.Validators;

/// <summary>
/// Validator for CreateCompanyRequest
/// </summary>
public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    private static readonly Regex VatNumberRegex = new(@"^[A-Z]{2}-\d{10,15}$", RegexOptions.Compiled);
    private static readonly Regex E164PhoneRegex = new(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled);

    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(255);

        RuleFor(x => x.VatNumber)
            .Must(BeValidVatNumber).When(x => !string.IsNullOrEmpty(x.VatNumber))
            .WithMessage("VAT number must be in format: {Country Code}-{10-15 digits} (e.g., TH-1234567890)");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Contact email must be a valid email address (RFC 5322)")
            .MaximumLength(255);

        RuleFor(x => x.ContactPhone)
            .Must(BeValidE164Phone).When(x => !string.IsNullOrEmpty(x.ContactPhone))
            .WithMessage("Contact phone must be in E.164 format");

        RuleFor(x => x.Segment)
            .NotEmpty().WithMessage("Segment is required")
            .Must(BeValidSegment).WithMessage($"Segment must be one of: {string.Join(", ", CustomerSegment.All)}");

        RuleFor(x => x.Tier)
            .NotEmpty().WithMessage("Tier is required")
            .Must(BeValidTier).WithMessage($"Tier must be one of: {string.Join(", ", CustomerTier.All)}");
    }

    private bool BeValidVatNumber(string? vatNumber)
    {
        return vatNumber == null || VatNumberRegex.IsMatch(vatNumber);
    }

    private bool BeValidE164Phone(string? phone)
    {
        return phone == null || E164PhoneRegex.IsMatch(phone);
    }

    private bool BeValidSegment(string segment)
    {
        return CustomerSegment.All.Contains(segment);
    }

    private bool BeValidTier(string tier)
    {
        return CustomerTier.All.Contains(tier);
    }
}

/// <summary>
/// Validator for UpdateCompanyRequest
/// </summary>
public class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    private static readonly Regex VatNumberRegex = new(@"^[A-Z]{2}-\d{10,15}$", RegexOptions.Compiled);
    private static readonly Regex E164PhoneRegex = new(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled);

    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.VatNumber)
            .Must(BeValidVatNumber).When(x => !string.IsNullOrEmpty(x.VatNumber))
            .WithMessage("VAT number must be in format: {Country Code}-{10-15 digits} (e.g., TH-1234567890)");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Contact email must be a valid email address (RFC 5322)")
            .MaximumLength(255);

        RuleFor(x => x.ContactPhone)
            .Must(BeValidE164Phone).When(x => !string.IsNullOrEmpty(x.ContactPhone))
            .WithMessage("Contact phone must be in E.164 format");

        RuleFor(x => x.Segment)
            .Must(BeValidSegment).When(x => !string.IsNullOrEmpty(x.Segment))
            .WithMessage($"Segment must be one of: {string.Join(", ", CustomerSegment.All)}");

        RuleFor(x => x.Tier)
            .Must(BeValidTier).When(x => !string.IsNullOrEmpty(x.Tier))
            .WithMessage($"Tier must be one of: {string.Join(", ", CustomerTier.All)}");

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required for updates");
    }

    private bool BeValidVatNumber(string? vatNumber)
    {
        return vatNumber == null || VatNumberRegex.IsMatch(vatNumber);
    }

    private bool BeValidE164Phone(string? phone)
    {
        return phone == null || E164PhoneRegex.IsMatch(phone);
    }

    private bool BeValidSegment(string? segment)
    {
        return segment != null && CustomerSegment.All.Contains(segment);
    }

    private bool BeValidTier(string? tier)
    {
        return tier != null && CustomerTier.All.Contains(tier);
    }
}
