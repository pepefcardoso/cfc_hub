using FluentValidation;

namespace CFCHub.Application.Finance.Queries.GetOverdueInstallments;

public class GetOverdueInstallmentsQueryValidator : AbstractValidator<GetOverdueInstallmentsQuery>
{
    public GetOverdueInstallmentsQueryValidator()
    {
    }
}