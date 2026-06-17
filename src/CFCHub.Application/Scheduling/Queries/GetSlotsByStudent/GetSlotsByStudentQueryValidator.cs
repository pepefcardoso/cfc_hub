using FluentValidation;

namespace CFCHub.Application.Scheduling.Queries.GetSlotsByStudent;

public class GetSlotsByStudentQueryValidator : AbstractValidator<GetSlotsByStudentQuery>
{
    public GetSlotsByStudentQueryValidator()
    {
    }
}