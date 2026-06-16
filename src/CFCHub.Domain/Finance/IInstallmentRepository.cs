using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Finance;

public interface IInstallmentRepository
{
    Task<bool> HasOverdueInstallmentsAsync(IEnumerable<EnrollmentId> enrollmentIds, CancellationToken cancellationToken = default);
    Task<Installment?> GetByIdAsync(InstallmentId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Installment>> GetByEnrollmentIdAsync(EnrollmentId enrollmentId, CancellationToken cancellationToken = default);
    Task AddAsync(Installment installment, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Installment> installments, CancellationToken cancellationToken = default);
    Task<PagedResult<Installment>> ListOverdueAsync(string? cursor, int limit, CancellationToken cancellationToken = default);
}
