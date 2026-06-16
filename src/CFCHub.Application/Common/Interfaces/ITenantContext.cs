using System;

namespace CFCHub.Application.Common.Interfaces;

public interface ITenantContext
{
    string SchemaName { get; }
    string TenantSlug { get; }
    Guid TenantId { get; }
    bool IsResolved { get; }
}
