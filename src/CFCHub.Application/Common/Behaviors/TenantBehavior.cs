using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Common.Behaviors;

public class TenantBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ITenantContext _tenantContext;

    public TenantBehavior(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_tenantContext.IsResolved)
        {
            throw new UnauthorizedException("Tenant not resolved.", "TENANT_NOT_RESOLVED");
        }

        return await next();
    }
}
