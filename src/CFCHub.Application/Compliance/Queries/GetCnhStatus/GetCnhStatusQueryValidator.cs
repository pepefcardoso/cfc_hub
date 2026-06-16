using System.Text.RegularExpressions;
using FluentValidation;

namespace CFCHub.Application.Compliance.Queries.GetCnhStatus;

public class GetCnhStatusQueryValidator : AbstractValidator<GetCnhStatusQuery>
{
    public GetCnhStatusQueryValidator()
    {
        RuleFor(x => x.Cpf)
            .NotEmpty().WithMessage("CPF is required.")
            .Matches(@"^\d{11}$").WithMessage("Invalid CPF format.");
    }
}
