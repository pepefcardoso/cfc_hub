using FluentValidation;

namespace CFCHub.Application.Enrollment.Queries.GetEnrollments;

public class GetEnrollmentsQueryValidator : AbstractValidator<GetEnrollmentsQuery>
{
    public GetEnrollmentsQueryValidator()
    {
    }
}