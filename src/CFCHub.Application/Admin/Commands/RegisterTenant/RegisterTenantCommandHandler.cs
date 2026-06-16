using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Admin.Commands.RegisterTenant;

public class RegisterTenantCommandHandler : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    private readonly ITenantRegistry _tenantRegistry;
    private readonly ITenantProvisioningService _provisioningService;
    private readonly ICurrentUserService _currentUserService;

    public RegisterTenantCommandHandler(
        ITenantRegistry tenantRegistry,
        ITenantProvisioningService provisioningService,
        ICurrentUserService currentUserService)
    {
        _tenantRegistry = tenantRegistry;
        _provisioningService = provisioningService;
        _currentUserService = currentUserService;
    }

    public async Task<RegisterTenantResult> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != RoleType.DetranAdmin)
        {
            throw new ForbiddenException("Apenas o super-administrador pode registrar novos tenants.", "FORBIDDEN");
        }

        var isUnique = await _tenantRegistry.IsSlugUniqueAsync(request.Slug, cancellationToken);
        if (!isUnique)
        {
            throw new ConflictException($"O slug '{request.Slug}' já está em uso.", "TENANT_SLUG_CONFLICT");
        }

        var tenantId = Guid.NewGuid();
        var schemaName = $"cfc_{request.Slug}";

        await _provisioningService.ProvisionAsync(request.Slug, tenantId, cancellationToken);

        return new RegisterTenantResult(tenantId, schemaName, request.Slug);
    }
}
