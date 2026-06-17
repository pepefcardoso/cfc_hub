using FluentValidation;

namespace CFCHub.Application.Scheduling.Queries.GetSlotsByInstructor;

public class GetSlotsByInstructorQueryValidator : AbstractValidator<GetSlotsByInstructorQuery>
{
    public GetSlotsByInstructorQueryValidator()
    {
    }
}