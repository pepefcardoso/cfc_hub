using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Domain.Finance;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<PagedResult<Payment>> ListAsync(string? cursor, int limit, CancellationToken cancellationToken = default);
}
