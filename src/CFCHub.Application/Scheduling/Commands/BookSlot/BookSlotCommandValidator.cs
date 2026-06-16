using System;
using CFCHub.Domain.Shared;
using FluentValidation;

namespace CFCHub.Application.Scheduling.Commands.BookSlot;

public class BookSlotCommandValidator : AbstractValidator<BookSlotCommand>
{
    public BookSlotCommandValidator(ISystemClock clock)
    {
        RuleFor(x => x.InstructorId).NotEmpty();
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.TrackId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
        
        RuleFor(x => x.StartedAt)
            .GreaterThan(clock.UtcNow).WithMessage("StartedAt must be in the future.")
            .Must(x => x.Minute == 0 || x.Minute == 50).WithMessage("Slot time must be on a 50-minute boundary (e.g., :00 or :50).");
    }
}
