using System;
using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Common.Interfaces;

public interface ITenantProvisioningService
{
    Task ProvisionAsync(string slug, Guid tenantId, CancellationToken ct = default);
}
