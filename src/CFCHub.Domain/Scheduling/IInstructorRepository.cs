using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public interface IInstructorRepository
{
    Task<Instructor?> GetByIdAsync(InstructorId id, CancellationToken ct);
    Task<PagedResult<Projections.InstructorProjection>> ListAsync(string? cursor, int limit, CancellationToken ct);
}
