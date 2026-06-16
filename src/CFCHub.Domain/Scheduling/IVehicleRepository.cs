using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken ct);
    Task<PagedResult<Projections.VehicleProjection>> ListAsync(string? cursor, int limit, CancellationToken ct);
}
