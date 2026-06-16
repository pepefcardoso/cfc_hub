using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling.Projections;

public record SlotProjection(
    Guid Id,
    Guid InstructorId,
    string InstructorName,
    Guid VehicleId,
    Guid TrackId,
    TrackType TrackType,
    Guid StudentId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    SlotStatus Status,
    CnhCategory Category);
