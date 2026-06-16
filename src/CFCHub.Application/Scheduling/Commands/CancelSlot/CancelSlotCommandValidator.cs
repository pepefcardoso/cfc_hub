using FluentValidation;

namespace CFCHub.Application.Scheduling.Commands.CancelSlot;

public class CancelSlotCommandValidator : AbstractValidator<CancelSlotCommand>
{
    public CancelSlotCommandValidator()
    {
        RuleFor(x => x.SlotId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
