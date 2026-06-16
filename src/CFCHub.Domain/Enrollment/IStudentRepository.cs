using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(StudentId id, CancellationToken cancellationToken = default);
    Task<Student?> GetByCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task AddAsync(Student student, CancellationToken cancellationToken = default);
    Task UpdateAsync(Student student, CancellationToken cancellationToken = default);
    Task<PagedResult<Student>> ListAsync(int pageSize, string? cursor, CancellationToken cancellationToken = default);
}
