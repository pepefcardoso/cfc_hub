using CFCHub.Domain.Compliance;
using FluentValidation;

namespace CFCHub.Application.Compliance.Commands.RegisterDocument;

public class RegisterDocumentCommandValidator : AbstractValidator<RegisterDocumentCommand>
{
    public RegisterDocumentCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("StudentId is required.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid document type.");

        RuleFor(x => x.ExpiryDate)
            .NotEmpty().WithMessage("ExpiryDate is required.");
    }
}
