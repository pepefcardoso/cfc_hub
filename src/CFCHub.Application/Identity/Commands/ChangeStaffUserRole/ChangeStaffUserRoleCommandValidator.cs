using FluentValidation;

namespace CFCHub.Application.Identity.Commands.ChangeStaffUserRole;

public class ChangeStaffUserRoleCommandValidator : AbstractValidator<ChangeStaffUserRoleCommand>
{
    public ChangeStaffUserRoleCommandValidator()
    {
    }
}