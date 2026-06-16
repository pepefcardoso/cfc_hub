using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling.Events;

public sealed record SchedulingSlotCompletedEvent(
    SchedulingSlotId SlotId,
    DateTimeOffset OccurredAt) : IDomainEvent;
