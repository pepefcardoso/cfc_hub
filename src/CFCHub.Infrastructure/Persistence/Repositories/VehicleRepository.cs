using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Scheduling.Projections;
using CFCHub.Domain.Shared;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly AppDbContext _context;

    public VehicleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Vehicle?> GetByIdAsync(VehicleId id, CancellationToken ct)
    {
        return await _context.Set<Vehicle>()
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<PagedResult<VehicleProjection>> ListAsync(string? cursor, int limit, CancellationToken ct)
    {
        var query = _context.Set<Vehicle>()
            .AsNoTracking();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            query = query.Where(v => v.Id.Value.CompareTo(cursorId) > 0);
        }

        var items = await query
            .OrderBy(v => v.Id)
            .Select(v => new VehicleProjection(
                v.Id.Value,
                v.LicensePlate,
                v.Category,
                v.Status))
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(limit);
        }

        string? nextCursor = items.Count > 0 ? items[^1].Id.ToString() : null;

        return new PagedResult<VehicleProjection>(items, nextCursor, hasMore, items.Count);
    }
}
