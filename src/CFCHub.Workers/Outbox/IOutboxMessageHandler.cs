using System.Threading;
using System.Threading.Tasks;

namespace CFCHub.Workers.Outbox;

public interface IOutboxMessageHandler<T> where T : class
{
    Task HandleAsync(T payload, CancellationToken ct);
}
