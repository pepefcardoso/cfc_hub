using System;
using System.Collections.Generic;
using System.Linq;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public class DayAvailabilityOverride : ValueObject
{
    public DateOnly Date { get; }
    public bool IsAvailable { get; }
    public IReadOnlyList<TimeWindow> CustomWindows { get; }

    public DayAvailabilityOverride(DateOnly date, bool isAvailable, IEnumerable<TimeWindow>? customWindows = null)
    {
        Date = date;
        IsAvailable = isAvailable;
        CustomWindows = customWindows?.ToList().AsReadOnly() ?? new List<TimeWindow>().AsReadOnly();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Date;
        yield return IsAvailable;
        foreach (var window in CustomWindows.OrderBy(w => w.Start))
        {
            yield return window;
        }
    }
}

public class TimeWindow : ValueObject
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public TimeWindow(TimeOnly start, TimeOnly end)
    {
        if (start >= end)
            throw new ArgumentException("Start time must be before end time.");

        Start = start;
        End = end;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
