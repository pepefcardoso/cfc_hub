using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Scheduling.Projections;
using CFCHub.Domain.Shared;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class InstructorRepository : IInstructorRepository
{
    private readonly AppDbContext _context;

    public InstructorRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Instructor?> GetByIdAsync(InstructorId id, CancellationToken ct)
    {
        // WeeklyTemplate is an owned entity, it's in the same row, so EF Core includes it by default if configured properly.
        // No explicit Include() needed for owned entities mapped to same table.
        return await _context.Set<Instructor>()
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<PagedResult<InstructorProjection>> ListAsync(string? cursor, int limit, CancellationToken ct)
    {
        var query = _context.Set<Instructor>()
            .AsNoTracking();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            query = query.Where(i => i.Id.Value.CompareTo(cursorId) > 0);
        }

        var items = await query
            .OrderBy(i => i.Id)
            .Select(i => new InstructorProjection(
                i.Id.Value,
                i.Name,
                i.TeachableCategories))
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(limit);
        }

        string? nextCursor = items.Count > 0 ? items[^1].Id.ToString() : null;

        return new PagedResult<InstructorProjection>(items, nextCursor, hasMore, items.Count);
    }
}
