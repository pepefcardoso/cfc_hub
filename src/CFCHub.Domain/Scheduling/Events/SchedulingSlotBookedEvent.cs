using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;

namespace CFCHub.Domain.Scheduling.Events;

public sealed record SchedulingSlotBookedEvent(
    SchedulingSlotId SlotId,
    InstructorId InstructorId,
    VehicleId VehicleId,
    TrackId TrackId,
    StudentId StudentId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    DateTimeOffset OccurredAt) : IDomainEvent;
