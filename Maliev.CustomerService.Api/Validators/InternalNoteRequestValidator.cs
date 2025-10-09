using FluentValidation;
using Maliev.CustomerService.Api.Models.InternalNotes;
using Maliev.CustomerService.Data.Models;

namespace Maliev.CustomerService.Api.Validators;

/// <summary>
/// Validator for CreateInternalNoteRequest
/// </summary>
public class CreateInternalNoteRequestValidator : AbstractValidator<CreateInternalNoteRequest>
{
    public CreateInternalNoteRequestValidator()
    {
        RuleFor(x => x.OwnerType)
            .NotEmpty().WithMessage("Owner type is required")
            .Must(BeValidOwnerType).WithMessage($"Owner type must be one of: {string.Join(", ", OwnerType.All)}");

        RuleFor(x => x.OwnerId)
            .NotEmpty().WithMessage("Owner ID is required");

        RuleFor(x => x.NoteText)
            .NotEmpty().WithMessage("Note text is required")
            .MaximumLength(5000).WithMessage("Note text must not exceed 5000 characters");
    }

    private bool BeValidOwnerType(string ownerType)
    {
        return OwnerType.All.Contains(ownerType);
    }
}

/// <summary>
/// Validator for UpdateInternalNoteRequest
/// </summary>
public class UpdateInternalNoteRequestValidator : AbstractValidator<UpdateInternalNoteRequest>
{
    public UpdateInternalNoteRequestValidator()
    {
        RuleFor(x => x.NoteText)
            .NotEmpty().WithMessage("Note text is required")
            .MaximumLength(5000).WithMessage("Note text must not exceed 5000 characters");

        RuleFor(x => x.Version)
            .NotEmpty().WithMessage("Version is required for updates");
    }
}
