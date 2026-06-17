using FluentValidation;

namespace CFCHub.Application.Enrollment.Queries.GetStudents;

public class GetStudentsQueryValidator : AbstractValidator<GetStudentsQuery>
{
    public GetStudentsQueryValidator()
    {
    }
}