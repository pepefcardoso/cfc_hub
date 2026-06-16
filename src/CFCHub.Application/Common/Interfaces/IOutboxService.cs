using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Application.Common.Interfaces;

public interface IOutboxService
{
    Task InsertAsync(string type, string payload, CancellationToken cancellationToken = default);
}
