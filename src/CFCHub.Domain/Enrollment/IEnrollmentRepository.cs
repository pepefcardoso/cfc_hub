using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(EnrollmentId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default);
    Task AddAsync(Enrollment enrollment, CancellationToken cancellationToken = default);
    Task<PagedResult<Enrollment>> ListAsync(int limit, string? cursor = null, CancellationToken cancellationToken = default);
}
