using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling.Events;

public sealed record SchedulingSlotCancelledEvent(
    SchedulingSlotId SlotId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
