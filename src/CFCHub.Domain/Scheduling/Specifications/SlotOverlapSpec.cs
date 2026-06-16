using System;

namespace CFCHub.Domain.Scheduling.Specifications;

public sealed class SlotOverlapSpec
{
    public bool IsSatisfiedBy(SchedulingSlot existing, DateTimeOffset newStart)
    {
        var newEnd = newStart.AddMinutes(50);
        return newStart < existing.EndedAt && existing.StartedAt < newEnd;
    }
}
