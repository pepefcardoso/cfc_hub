using System;
using System.Collections.Generic;
using System.Linq;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public class InstructorAvailabilityTemplate : ValueObject
{
    public IReadOnlyList<AvailabilityWindow> Windows { get; }

    public InstructorAvailabilityTemplate(IEnumerable<AvailabilityWindow> windows)
    {
        Windows = windows.ToList().AsReadOnly();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private InstructorAvailabilityTemplate() { }
#pragma warning restore CS8618

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var window in Windows.OrderBy(w => w.DayOfWeek).ThenBy(w => w.Start))
        {
            yield return window;
        }
    }
}

public class AvailabilityWindow : ValueObject
{
    public DayOfWeek DayOfWeek { get; }
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public AvailabilityWindow(DayOfWeek dayOfWeek, TimeOnly start, TimeOnly end)
    {
        if (start >= end)
            throw new ArgumentException("Start time must be before end time.");

        DayOfWeek = dayOfWeek;
        Start = start;
        End = end;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private AvailabilityWindow() { }
#pragma warning restore CS8618

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DayOfWeek;
        yield return Start;
        yield return End;
    }
}
