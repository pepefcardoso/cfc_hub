using FluentValidation;

namespace CFCHub.Application.Identity.Commands.DeactivateStaffUser;

public class DeactivateStaffUserCommandValidator : AbstractValidator<DeactivateStaffUserCommand>
{
    public DeactivateStaffUserCommandValidator()
    {
    }
}