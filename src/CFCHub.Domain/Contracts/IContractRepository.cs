using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Domain.Contracts;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(ContractId id, CancellationToken cancellationToken = default);
    Task<Contract?> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default);
    Task AddAsync(Contract contract, CancellationToken cancellationToken = default);
    Task UpdateAsync(Contract contract, CancellationToken cancellationToken = default);
}
