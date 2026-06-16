using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Admin.Queries.GetTenant;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantResult>
{
    private readonly ITenantRegistry _tenantRegistry;

    public GetTenantQueryHandler(ITenantRegistry tenantRegistry)
    {
        _tenantRegistry = tenantRegistry;
    }

    public async Task<TenantResult> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRegistry.GetBySlugAsync(request.Slug, cancellationToken);
        
        if (tenant == null)
        {
            throw new TenantNotFoundException(request.Slug);
        }

        return new TenantResult(
            tenant.Id,
            tenant.Slug,
            tenant.SchemaName,
            tenant.Status
        );
    }
}
