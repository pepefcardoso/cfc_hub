using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling.Events;

public sealed record SchedulingSlotReminderRequestedEvent(
    SchedulingSlotId SlotId,
    Guid StudentId,
    DateTimeOffset StartedAt,
    DateTimeOffset OccurredAt) : IDomainEvent;
