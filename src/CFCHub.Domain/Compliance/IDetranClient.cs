using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Domain.Compliance;

public interface IDetranClient
{
    Task<CnhStatusResult> GetCnhStatusAsync(string cpf, CancellationToken cancellationToken = default);
}
