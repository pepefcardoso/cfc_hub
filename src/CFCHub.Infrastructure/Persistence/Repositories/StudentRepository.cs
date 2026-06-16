using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;

namespace CFCHub.Infrastructure.Persistence.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly AppDbContext _context;

    public StudentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByIdAsync(StudentId id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Student>().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Student?> GetByCpfAsync(string cpf, CancellationToken cancellationToken = default)
    {
        var hash = ComputeCpfHash(cpf);
        return await _context.Set<Student>()
            .FirstOrDefaultAsync(s => EF.Property<string>(s, "CpfHash") == hash, cancellationToken);
    }

    public Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        _context.Set<Student>().Add(student);
        _context.Entry(student).Property("CpfHash").CurrentValue = ComputeCpfHash(student.Cpf);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        _context.Set<Student>().Update(student);
        _context.Entry(student).Property("CpfHash").CurrentValue = ComputeCpfHash(student.Cpf);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<Student>> ListAsync(int pageSize, string? cursor, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Student>().AsNoTracking();

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
        {
            query = query.Where(s => s.Id.Value.CompareTo(cursorId) > 0);
        }

        var items = await query
            .OrderBy(s => s.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(pageSize);
        }

        string? nextCursor = items.Count > 0 ? items[^1].Id.ToString() : null;

        return new PagedResult<Student>(items, nextCursor, hasMore, items.Count);
    }

    private static string ComputeCpfHash(string cpf)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(cpf));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
