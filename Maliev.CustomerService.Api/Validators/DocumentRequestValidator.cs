using FluentValidation;
using Maliev.CustomerService.Api.Models.Documents;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Api.Validators;

/// <summary>
/// Validator for CreateDocumentRequest
/// </summary>
public class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequest>
{
    public CreateDocumentRequestValidator()
    {
        RuleFor(x => x.OwnerType)
            .NotEmpty().WithMessage("Owner type is required")
            .Must(BeValidOwnerType).WithMessage($"Owner type must be one of: {string.Join(", ", OwnerType.All)}");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required");

        RuleFor(x => x.DocumentType)
            .NotEmpty().WithMessage("Document type is required")
            .MaximumLength(100).WithMessage("Document type must not exceed 100 characters");

        RuleFor(x => x.FileReference)
            .NotEmpty().WithMessage("File reference is required")
            .MaximumLength(500).WithMessage("File reference must not exceed 500 characters");

        RuleFor(x => x.Filename)
            .NotEmpty().WithMessage("Filename is required")
            .MaximumLength(255).WithMessage("Filename must not exceed 255 characters");
    }

    private bool BeValidOwnerType(string ownerType)
    {
        return OwnerType.All.Contains(ownerType);
    }
}

/// <summary>
/// Validator for UpdateDocumentRequest
/// </summary>
public class UpdateDocumentRequestValidator : AbstractValidator<UpdateDocumentRequest>
{
    public UpdateDocumentRequestValidator()
    {
        RuleFor(x => x.FileReference)
            .NotEmpty().WithMessage("File reference is required")
            .MaximumLength(500).WithMessage("File reference must not exceed 500 characters");

        RuleFor(x => x.Filename)
            .NotEmpty().WithMessage("Filename is required")
            .MaximumLength(255).WithMessage("Filename must not exceed 255 characters");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("Row version is required for updates");
    }
}
