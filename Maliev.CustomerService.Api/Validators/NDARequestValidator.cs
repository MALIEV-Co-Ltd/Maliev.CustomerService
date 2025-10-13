using FluentValidation;
using Maliev.CustomerService.Api.Models.NDAs;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Api.Validators;

/// <summary>
/// Validator for CreateNDARequest
/// </summary>
public class CreateNDARequestValidator : AbstractValidator<CreateNDARequest>
{
    public CreateNDARequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.ExpiresAt)
            .Must(BeFutureDate).When(x => x.ExpiresAt.HasValue)
            .WithMessage("Expiration date must be in the future");
    }

    private bool BeFutureDate(DateTime? expiresAt)
    {
        return expiresAt == null || expiresAt.Value > DateTime.UtcNow;
    }
}

/// <summary>
/// Validator for UpdateNDAStatusRequest with lifecycle transition validation
/// </summary>
public class UpdateNDAStatusRequestValidator : AbstractValidator<UpdateNDAStatusRequest>
{
    public UpdateNDAStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(BeValidStatus).WithMessage($"Status must be one of: {string.Join(", ", NDAStatus.All)}");

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required for updates");

        // When transitioning to Signed, signedBy and signedAt are required
        When(x => x.Status == NDAStatus.Signed, () =>
        {
            RuleFor(x => x.SignedBy)
                .NotEmpty().WithMessage("SignedBy is required when transitioning to Signed status");

            RuleFor(x => x.SignedAt)
                .NotNull().WithMessage("SignedAt is required when transitioning to Signed status");
        });

        // When transitioning to Revoked, revokedAt is required
        When(x => x.Status == NDAStatus.Revoked, () =>
        {
            RuleFor(x => x.RevokedAt)
                .NotNull().WithMessage("RevokedAt is required when transitioning to Revoked status");
        });
    }

    private bool BeValidStatus(string status)
    {
        return NDAStatus.All.Contains(status);
    }
}
