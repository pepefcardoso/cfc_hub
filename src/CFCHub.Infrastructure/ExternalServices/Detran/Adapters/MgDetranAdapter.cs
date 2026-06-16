using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Compliance;

namespace CFCHub.Infrastructure.ExternalServices.Detran.Adapters;

public class MgDetranAdapter : IDetranAdapter
{
    public Task<CnhStatusResult> GetCnhStatusAsync(string cpf, CancellationToken cancellationToken = default)
    {
        // Calls Playwright sidecar via gRPC (Placeholder)
        return Task.FromResult(CnhStatusResult.Unavailable);
    }
}
