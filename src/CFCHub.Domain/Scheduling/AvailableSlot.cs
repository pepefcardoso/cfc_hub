using System;

namespace CFCHub.Domain.Scheduling;

public sealed record AvailableSlot(
    DateTimeOffset StartedAt,
    InstructorId InstructorId,
    VehicleId VehicleId,
    TrackId TrackId
);
