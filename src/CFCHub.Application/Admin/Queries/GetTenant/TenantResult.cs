using System;

namespace CFCHub.Application.Admin.Queries.GetTenant;

public record TenantResult(
    Guid Id,
    string Slug,
    string SchemaName,
    string Status
);
