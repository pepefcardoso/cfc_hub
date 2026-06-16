using System;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;

namespace CFCHub.Application.Scheduling.Queries.GetSlotById;

public record SlotDetailsResult(
    Guid Id,
    Guid InstructorId,
    Guid VehicleId,
    Guid TrackId,
    Guid StudentId,
    CnhCategory Category,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    SlotStatus Status,
    string? CancellationReason
);
