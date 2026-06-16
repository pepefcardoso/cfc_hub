using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Domain.Enrollment;

public interface IDataErasureRequestRepository
{
    Task<DataErasureRequest?> GetByIdAsync(DataErasureRequestId id, CancellationToken cancellationToken = default);
    Task<DataErasureRequest?> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default);
    Task AddAsync(DataErasureRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(DataErasureRequest request, CancellationToken cancellationToken = default);
}
