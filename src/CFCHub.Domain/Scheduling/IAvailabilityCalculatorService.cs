using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public interface IAvailabilityCalculatorService
{
    Task<PagedResult<AvailableSlot>> GetAvailableSlotsAsync(
        DateOnly date, 
        CnhCategory category, 
        InstructorId? instructorId, 
        string? cursor, 
        int limit, 
        CancellationToken ct);
}
