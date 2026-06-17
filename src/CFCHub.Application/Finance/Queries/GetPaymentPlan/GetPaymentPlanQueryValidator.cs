using FluentValidation;

namespace CFCHub.Application.Finance.Queries.GetPaymentPlan;

public class GetPaymentPlanQueryValidator : AbstractValidator<GetPaymentPlanQuery>
{
    public GetPaymentPlanQueryValidator()
    {
    }
}