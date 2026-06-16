using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly AppDbContext _context;

    public EnrollmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(EnrollmentId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Enrollment>()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Enrollment>()
            .Where(e => e.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default)
    {
        _context.Set<Enrollment>().Add(enrollment);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<Enrollment>> ListAsync(int limit, string? cursor = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Enrollment>().AsNoTracking();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            query = query.Where(e => e.Id.Value.CompareTo(cursorId) > 0);
        }

        var items = await query
            .OrderBy(e => e.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(limit);
        }

        string? nextCursor = items.Count > 0 ? items[^1].Id.ToString() : null;

        return new PagedResult<Enrollment>(items, nextCursor, hasMore, items.Count);
    }
}
