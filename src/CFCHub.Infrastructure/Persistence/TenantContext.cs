using System;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared.Exceptions;

namespace CFCHub.Infrastructure.Persistence;

public class TenantContext : ITenantContext
{
    private string? _schemaName;
    private string? _tenantSlug;
    private Guid? _tenantId;

    public string SchemaName => IsResolved ? _schemaName! : throw new InfrastructureException("TENANT_NOT_RESOLVED");
    public string TenantSlug => IsResolved ? _tenantSlug! : throw new InfrastructureException("TENANT_NOT_RESOLVED");
    public Guid TenantId => IsResolved ? _tenantId!.Value : throw new InfrastructureException("TENANT_NOT_RESOLVED");
    
    public bool IsResolved { get; private set; }

    public void Resolve(string schemaName, string slug, Guid tenantId)
    {
        _schemaName = schemaName;
        _tenantSlug = slug;
        _tenantId = tenantId;
        IsResolved = true;
    }
}
