using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling.Projections;

public record VehicleProjection(
    Guid Id,
    string LicensePlate,
    CnhCategory Category,
    VehicleStatus Status);
