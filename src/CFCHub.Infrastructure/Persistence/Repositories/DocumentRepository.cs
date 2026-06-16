using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Compliance;
using CFCHub.Domain.Students;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _context;

    public DocumentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentRecord>> GetExpiringAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-1);

        return await _context.Set<DocumentRecord>()
            .Where(d => d.ExpiryDate >= from && d.ExpiryDate <= to &&
                        (d.LastAlertSentAt == null || d.LastAlertSentAt < cutoff))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<DocumentRecord>> GetByStudentIdAsync(StudentId studentId, CancellationToken ct = default)
    {
        return await _context.Set<DocumentRecord>()
            .Where(d => d.StudentId == studentId)
            .ToListAsync(ct);
    }

    public Task AddAsync(DocumentRecord record, CancellationToken ct = default)
    {
        _context.Set<DocumentRecord>().Add(record);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DocumentRecord record, CancellationToken ct = default)
    {
        _context.Set<DocumentRecord>().Update(record);
        return Task.CompletedTask;
    }
}
