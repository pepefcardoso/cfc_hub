using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Common.Interfaces;

public record TenantRecord(Guid Id, string Slug, string SchemaName, string Status);

public interface ITenantRegistry
{
    Task<bool> IsSlugUniqueAsync(string slug, CancellationToken ct = default);
    Task<TenantRecord?> GetBySlugAsync(string slug, CancellationToken ct = default);
}
