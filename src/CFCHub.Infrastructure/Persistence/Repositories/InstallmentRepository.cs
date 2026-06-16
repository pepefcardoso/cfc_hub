using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;

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
}
