using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Pagination;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class StaffUserRepository : IStaffUserRepository
{
    private readonly AppDbContext _context;

    public StaffUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StaffUser?> GetByIdAsync(StaffUserId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StaffUser>()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StaffUser>()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task AddAsync(StaffUser user, CancellationToken cancellationToken = default)
    {
        _context.Set<StaffUser>().Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(StaffUser user, CancellationToken cancellationToken = default)
    {
        _context.Set<StaffUser>().Update(user);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<StaffUser>> ListAsync(Cursor? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<StaffUser>().AsNoTracking();

        if (cursor != null)
        {
            query = query.Where(u => u.Id.Value.CompareTo(cursor.Id) > 0);
        }

        var items = await query
            .OrderBy(u => u.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(limit);
        }

        string? nextCursor = items.Count > 0 ? new Cursor(items[^1].Id.Value, DateTimeOffset.UtcNow).Encode() : null;

        return new PagedResult<StaffUser>(items, nextCursor, hasMore, items.Count);
    }
}
