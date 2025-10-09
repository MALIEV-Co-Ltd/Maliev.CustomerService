using FluentValidation;
using Maliev.CustomerService.Api.Models.Addresses;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Api.Validators;

/// <summary>
/// Validator for CreateAddressRequest
/// </summary>
public class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.OwnerType)
            .NotEmpty().WithMessage("Owner type is required")
            .Must(BeValidOwnerType).WithMessage($"Owner type must be one of: {string.Join(", ", OwnerType.All)}");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Address type is required")
            .Must(BeValidAddressType).WithMessage($"Address type must be one of: {string.Join(", ", AddressType.All)}");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required")
            .MaximumLength(255);

        RuleFor(x => x.AddressLine2)
            .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.AddressLine2));

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100);

        RuleFor(x => x.Province)
            .NotEmpty().WithMessage("Province is required")
            .MaximumLength(100);

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required")
            .MaximumLength(20);

        RuleFor(x => x.CountryId)
            .NotEmpty().WithMessage("Country ID is required");
    }

    private bool BeValidOwnerType(string ownerType)
    {
        return OwnerType.All.Contains(ownerType);
    }

    private bool BeValidAddressType(string addressType)
    {
        return AddressType.All.Contains(addressType);
    }
}

/// <summary>
/// Validator for UpdateAddressRequest
/// </summary>
public class UpdateAddressRequestValidator : AbstractValidator<UpdateAddressRequest>
{
    public UpdateAddressRequestValidator()
    {
        RuleFor(x => x.Type)
            .Must(BeValidAddressType).When(x => !string.IsNullOrEmpty(x.Type))
            .WithMessage($"Address type must be one of: {string.Join(", ", AddressType.All)}");

        RuleFor(x => x.AddressLine1)
            .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.AddressLine1));

        RuleFor(x => x.AddressLine2)
            .MaximumLength(255).When(x => !string.IsNullOrEmpty(x.AddressLine2));

        RuleFor(x => x.City)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.Province)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Province));

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required for updates");
    }

    private bool BeValidAddressType(string? addressType)
    {
        return addressType != null && AddressType.All.Contains(addressType);
    }
}
