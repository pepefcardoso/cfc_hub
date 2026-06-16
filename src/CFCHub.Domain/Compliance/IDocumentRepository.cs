using CFCHub.Domain.Students;

namespace CFCHub.Domain.Compliance;

public interface IDocumentRepository
{
    Task<IEnumerable<DocumentRecord>> GetExpiringAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IEnumerable<DocumentRecord>> GetByStudentIdAsync(StudentId studentId, CancellationToken ct = default);
    Task AddAsync(DocumentRecord record, CancellationToken ct = default);
    Task UpdateAsync(DocumentRecord record, CancellationToken ct = default);
}
