using FluentValidation;

namespace CFCHub.Application.Enrollment.Queries.GetStudent;

public class GetStudentQueryValidator : AbstractValidator<GetStudentQuery>
{
    public GetStudentQueryValidator()
    {
    }
}