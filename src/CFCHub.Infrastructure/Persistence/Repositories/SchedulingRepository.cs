using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Scheduling.Projections;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class SchedulingRepository : ISchedulingRepository
{
    private readonly AppDbContext _context;

    public SchedulingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SchedulingSlot?> GetSlotByIdAsync(SchedulingSlotId id, CancellationToken ct)
    {
        return await _context.Set<SchedulingSlot>()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<SchedulingSlot?> GetOverlappingInstructorSlotAsync(InstructorId instructorId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        return await _context.Set<SchedulingSlot>()
            .FromSqlRaw(
                "SELECT * FROM \"SchedulingSlots\" WHERE \"InstructorId\" = {0} AND \"StartedAt\" < {1} AND \"EndedAt\" > {2} AND \"Status\" != 1 FOR UPDATE", 
                instructorId.Value, end, start)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SchedulingSlot?> GetOverlappingVehicleSlotAsync(VehicleId vehicleId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        return await _context.Set<SchedulingSlot>()
            .FromSqlRaw(
                "SELECT * FROM \"SchedulingSlots\" WHERE \"VehicleId\" = {0} AND \"StartedAt\" < {1} AND \"EndedAt\" > {2} AND \"Status\" != 1 FOR UPDATE", 
                vehicleId.Value, end, start)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SchedulingSlot?> GetOverlappingTrackSlotAsync(TrackId trackId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        return await _context.Set<SchedulingSlot>()
            .FromSqlRaw(
                "SELECT * FROM \"SchedulingSlots\" WHERE \"TrackId\" = {0} AND \"StartedAt\" < {1} AND \"EndedAt\" > {2} AND \"Status\" != 1 FOR UPDATE", 
                trackId.Value, end, start)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(SchedulingSlot slot, CancellationToken ct)
    {
        await _context.Set<SchedulingSlot>().AddAsync(slot, ct);
    }

    public async Task<PagedResult<SlotProjection>> GetByInstructorAsync(InstructorId instructorId, string? cursor, int limit, CancellationToken ct)
    {
        var query = _context.Set<SchedulingSlot>()
            .AsNoTracking()
            .Where(s => s.InstructorId == instructorId);

        if (!string.IsNullOrEmpty(cursor))
        {
            var parts = cursor.Split('|');
            if (parts.Length == 2 && 
                long.TryParse(parts[0], out var ticks) && 
                Guid.TryParse(parts[1], out var cursorIdVal))
            {
                var cursorTs = new DateTimeOffset(ticks, TimeSpan.Zero);
                
                query = query.Where(s => s.StartedAt > cursorTs || (s.StartedAt == cursorTs && s.Id.Value.CompareTo(cursorIdVal) > 0));
            }
        }

        var items = await query
            .OrderBy(s => s.StartedAt)
            .ThenBy(s => s.Id)
            .Join(_context.Set<Instructor>(), s => s.InstructorId, i => i.Id, (s, i) => new { s, i.Name })
            .Join(_context.Set<Track>(), si => si.s.TrackId, t => t.Id, (si, t) => new { si.s, si.Name, t.Type })
            .Select(x => new SlotProjection(
                x.s.Id.Value,
                x.s.InstructorId.Value,
                x.Name,
                x.s.VehicleId.Value,
                x.s.TrackId.Value,
                x.Type,
                x.s.StudentId.Value,
                x.s.StartedAt,
                x.s.EndedAt,
                x.s.Status,
                x.s.Category))
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(limit);
        }

        string? nextCursor = null;
        if (items.Count > 0)
        {
            var lastItem = items[^1];
            nextCursor = $"{lastItem.StartedAt.UtcTicks}|{lastItem.Id}";
        }

        return new PagedResult<SlotProjection>(items, nextCursor, hasMore, items.Count);
    }

    public async Task<PagedResult<SlotProjection>> GetByStudentAsync(StudentId studentId, string? cursor, int limit, CancellationToken ct)
    {
        var query = _context.Set<SchedulingSlot>()
            .AsNoTracking()
            .Where(s => s.StudentId == studentId);

        if (!string.IsNullOrEmpty(cursor))
        {
            var parts = cursor.Split('|');
            if (parts.Length == 2 && 
                long.TryParse(parts[0], out var ticks) && 
                Guid.TryParse(parts[1], out var cursorIdVal))
            {
                var cursorTs = new DateTimeOffset(ticks, TimeSpan.Zero);
                
                query = query.Where(s => s.StartedAt > cursorTs || (s.StartedAt == cursorTs && s.Id.Value.CompareTo(cursorIdVal) > 0));
            }
        }

        var items = await query
            .OrderBy(s => s.StartedAt)
            .ThenBy(s => s.Id)
            .Join(_context.Set<Instructor>(), s => s.InstructorId, i => i.Id, (s, i) => new { s, i.Name })
            .Join(_context.Set<Track>(), si => si.s.TrackId, t => t.Id, (si, t) => new { si.s, si.Name, t.Type })
            .Select(x => new SlotProjection(
                x.s.Id.Value,
                x.s.InstructorId.Value,
                x.Name,
                x.s.VehicleId.Value,
                x.s.TrackId.Value,
                x.Type,
                x.s.StudentId.Value,
                x.s.StartedAt,
                x.s.EndedAt,
                x.s.Status,
                x.s.Category))
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(limit);
        }

        string? nextCursor = null;
        if (items.Count > 0)
        {
            var lastItem = items[^1];
            nextCursor = $"{lastItem.StartedAt.UtcTicks}|{lastItem.Id}";
        }

        return new PagedResult<SlotProjection>(items, nextCursor, hasMore, items.Count);
    }
}
