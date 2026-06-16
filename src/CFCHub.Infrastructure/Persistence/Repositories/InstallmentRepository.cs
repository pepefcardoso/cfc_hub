using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Shared;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class InstallmentRepository : IInstallmentRepository
{
    private readonly AppDbContext _context;

    public InstallmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasOverdueInstallmentsAsync(IEnumerable<EnrollmentId> enrollmentIds, CancellationToken cancellationToken = default)
    {
        var ids = enrollmentIds.ToList();
        return await _context.Set<Installment>()
            .AnyAsync(i => ids.Contains(i.EnrollmentId) && i.Status == InstallmentStatus.Overdue, cancellationToken);
    }

    public async Task<Installment?> GetByIdAsync(InstallmentId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Installment>()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Installment>> GetByEnrollmentIdAsync(EnrollmentId enrollmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Installment>()
            .Where(i => i.EnrollmentId == enrollmentId)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Installment installment, CancellationToken cancellationToken = default)
    {
        await _context.Set<Installment>().AddAsync(installment, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Installment> installments, CancellationToken cancellationToken = default)
    {
        await _context.Set<Installment>().AddRangeAsync(installments, cancellationToken);
    }

    public async Task<PagedResult<Installment>> ListOverdueAsync(string? cursor, int limit, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Installment>()
            .Where(i => i.Status == InstallmentStatus.Overdue);

        if (!string.IsNullOrEmpty(cursor))
        {
            query = query.Where(i => i.Id.Value.ToString().CompareTo(cursor) > 0);
        }

        var items = await query
            .OrderBy(i => i.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > limit;
        if (hasNextPage)
        {
            items.RemoveAt(limit);
        }

        var nextCursor = hasNextPage ? items.Last().Id.Value.ToString() : null;
        return new PagedResult<Installment>(items, nextCursor, hasNextPage, items.Count);
    }
}
