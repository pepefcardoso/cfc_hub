using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Payment>()
            .Where(p => p.StudentId == studentId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Set<Payment>().Add(payment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Set<Payment>().Update(payment);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<Payment>> ListAsync(string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Payment>().AsNoTracking();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            query = query.Where(p => p.Id.Value.CompareTo(cursorId) > 0);
        }

        var items = await query
            .OrderBy(p => p.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > limit;
        if (hasMore)
        {
            items.RemoveAt(limit);
        }

        string? nextCursor = items.Count > 0 ? items[^1].Id.ToString() : null;

        return new PagedResult<Payment>(items, nextCursor, hasMore, items.Count);
    }
}
