using FluentValidation;

namespace CFCHub.Application.Compliance.Queries.GetExpiringDocuments;

public class GetExpiringDocumentsQueryValidator : AbstractValidator<GetExpiringDocumentsQuery>
{
    public GetExpiringDocumentsQueryValidator()
    {
        RuleFor(x => x.From)
            .NotEmpty().WithMessage("From date is required.");

        RuleFor(x => x.To)
            .NotEmpty().WithMessage("To date is required.")
            .GreaterThanOrEqualTo(x => x.From).WithMessage("To date must be greater than or equal to From date.");
    }
}
