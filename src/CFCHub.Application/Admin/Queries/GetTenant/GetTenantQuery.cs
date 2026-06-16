using MediatR;

namespace CFCHub.Application.Admin.Queries.GetTenant;

public record GetTenantQuery(string Slug) : IRequest<TenantResult>;
