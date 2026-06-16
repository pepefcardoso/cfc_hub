using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Compliance;

namespace CFCHub.Infrastructure.ExternalServices.Detran.Adapters;

public class DefaultDetranAdapter : IDetranAdapter
{
    public Task<CnhStatusResult> GetCnhStatusAsync(string cpf, CancellationToken cancellationToken = default)
    {
        // Fallback for states without an explicit adapter
        return Task.FromResult(CnhStatusResult.Unavailable);
    }
}
