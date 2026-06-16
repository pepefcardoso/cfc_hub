using System;
using CFCHub.Domain.Scheduling;
using FluentValidation;
using CFCHub.Domain.Shared;

namespace CFCHub.Application.Scheduling.Queries.GetAvailableSlots;

public class GetAvailableSlotsQueryValidator : AbstractValidator<GetAvailableSlotsQuery>
{
    public GetAvailableSlotsQueryValidator(ISystemClock clock)
    {
        RuleFor(x => x.Date)
            .Must(date => date >= DateOnly.FromDateTime(clock.UtcNow.Date))
            .WithMessage("Date must not be in the past.");

        RuleFor(x => x.Limit)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Limit must be between 1 and 100.");
    }
}
