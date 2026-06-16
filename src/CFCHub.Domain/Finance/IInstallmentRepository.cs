using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Domain.Finance;

public interface IInstallmentRepository
{
    Task<bool> HasOverdueInstallmentsAsync(IEnumerable<EnrollmentId> enrollmentIds, CancellationToken cancellationToken = default);
}
