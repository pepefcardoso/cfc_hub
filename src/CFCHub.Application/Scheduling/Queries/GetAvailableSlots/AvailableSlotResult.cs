using System;
using CFCHub.Domain.Scheduling;

namespace CFCHub.Application.Scheduling.Queries.GetAvailableSlots;

public record AvailableSlotResult(
    DateTimeOffset StartedAt,
    Guid InstructorId,
    string InstructorName,
    Guid VehicleId,
    Guid TrackId,
    TrackType TrackType
);
