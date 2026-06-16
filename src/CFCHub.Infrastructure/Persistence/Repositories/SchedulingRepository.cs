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

    public async Task<SchedulingSlot?> GetOverlappingSlotAsync(InstructorId instructorId, DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        // PostgreSQL SELECT FOR UPDATE locks the row. EF Core translates FromSqlRaw correctly.
        // We use string interpolation or FromSqlRaw with parameters.
        return await _context.Set<SchedulingSlot>()
            .FromSqlRaw(
                "SELECT * FROM \"SchedulingSlots\" WHERE \"InstructorId\" = {0} AND \"StartedAt\" < {1} AND \"EndedAt\" > {2} FOR UPDATE", 
                instructorId.Value, end, start)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(SchedulingSlot slot, CancellationToken ct)
    {
        await _context.Set<SchedulingSlot>().AddAsync(slot, ct);
    }

    public async Task<IReadOnlyCollection<SlotProjection>> GetByInstructorAsync(InstructorId instructorId, DateOnly date, CancellationToken ct)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Set<SchedulingSlot>()
            .AsNoTracking()
            .Where(s => s.InstructorId == instructorId && s.StartedAt >= startOfDay && s.StartedAt < endOfDay)
            .Select(s => new SlotProjection(
                s.Id.Value,
                s.InstructorId.Value,
                s.VehicleId.Value,
                s.TrackId.Value,
                s.StudentId.Value,
                s.StartedAt,
                s.EndedAt,
                s.Status,
                s.Category))
            .ToListAsync(ct);
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
            .Select(s => new SlotProjection(
                s.Id.Value,
                s.InstructorId.Value,
                s.VehicleId.Value,
                s.TrackId.Value,
                s.StudentId.Value,
                s.StartedAt,
                s.EndedAt,
                s.Status,
                s.Category))
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
