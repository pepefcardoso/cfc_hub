using System;

namespace CFCHub.Domain.Scheduling;

public sealed record AvailableSlot(
    DateTimeOffset StartedAt,
    InstructorId InstructorId,
    string InstructorName,
    VehicleId VehicleId,
    TrackId TrackId,
    TrackType TrackType
);
