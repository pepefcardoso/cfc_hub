using System;

namespace CFCHub.Application.Admin.Commands.RegisterTenant;

public record RegisterTenantResult(
    Guid TenantId,
    string SchemaName,
    string Slug
);
