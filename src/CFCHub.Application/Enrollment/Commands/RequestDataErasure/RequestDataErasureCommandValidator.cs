using FluentValidation;

namespace CFCHub.Application.Enrollment.Commands.RequestDataErasure;

public class RequestDataErasureCommandValidator : AbstractValidator<RequestDataErasureCommand>
{
    public RequestDataErasureCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("StudentId is required.");
    }
}
