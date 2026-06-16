using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class DataErasureRequestRepository : IDataErasureRequestRepository
{
    private readonly AppDbContext _context;

    public DataErasureRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DataErasureRequest?> GetByIdAsync(DataErasureRequestId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DataErasureRequest>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<DataErasureRequest?> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DataErasureRequest>().FirstOrDefaultAsync(r => r.StudentId == studentId, cancellationToken);
    }

    public Task AddAsync(DataErasureRequest request, CancellationToken cancellationToken = default)
    {
        _context.Set<DataErasureRequest>().Add(request);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DataErasureRequest request, CancellationToken cancellationToken = default)
    {
        _context.Set<DataErasureRequest>().Update(request);
        return Task.CompletedTask;
    }
}
