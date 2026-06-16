using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Compliance;

namespace CFCHub.Infrastructure.ExternalServices.Detran.Adapters;

public interface IDetranAdapter
{
    Task<CnhStatusResult> GetCnhStatusAsync(string cpf, CancellationToken cancellationToken = default);
}
