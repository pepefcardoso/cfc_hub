using System;
using CFCHub.Domain.Scheduling;

namespace CFCHub.Application.Scheduling.Queries.GetSlotsByInstructor;

public record SlotResult(
    Guid Id,
    SlotStatus Status,
    DateTimeOffset StartedAt,
    string InstructorName,
    Guid VehicleId,
    TrackType TrackType
);
