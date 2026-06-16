using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;

namespace CFCHub.Domain.Scheduling;

public interface ISchedulingRepository
{
    Task<SchedulingSlot?> GetSlotByIdAsync(SchedulingSlotId id, CancellationToken ct);
    
    Task<SchedulingSlot?> GetOverlappingInstructorSlotAsync(InstructorId instructorId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
    Task<SchedulingSlot?> GetOverlappingVehicleSlotAsync(VehicleId vehicleId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
    Task<SchedulingSlot?> GetOverlappingTrackSlotAsync(TrackId trackId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct);
    Task AddAsync(SchedulingSlot slot, CancellationToken ct);
    
    Task<PagedResult<Projections.SlotProjection>> GetByInstructorAsync(InstructorId instructorId, string? cursor, int limit, CancellationToken ct);
    
    Task<PagedResult<Projections.SlotProjection>> GetByStudentAsync(StudentId studentId, string? cursor, int limit, CancellationToken ct);
}
