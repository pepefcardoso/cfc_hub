using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling.Projections;

public record SlotProjection(
    Guid Id,
    Guid InstructorId,
    Guid VehicleId,
    Guid TrackId,
    Guid StudentId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    SlotStatus Status,
    CnhCategory Category);
