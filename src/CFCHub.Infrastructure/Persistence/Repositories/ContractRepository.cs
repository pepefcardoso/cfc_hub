using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Enrollment;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly AppDbContext _context;

    public ContractRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Contract?> GetByIdAsync(ContractId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Contract>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Contract?> GetByStudentIdAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Contract>()
            .FirstOrDefaultAsync(c => c.StudentId == studentId, cancellationToken);
    }

    public Task AddAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        _context.Set<Contract>().Add(contract);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Contract contract, CancellationToken cancellationToken = default)
    {
        _context.Set<Contract>().Update(contract);
        return Task.CompletedTask;
    }
}
