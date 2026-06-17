using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Domain.Shared.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CFCHub.Workers.Outbox;

public interface IOutboxMessageDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken ct);
}

public class OutboxMessageDispatcher : IOutboxMessageDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxMessageDispatcher> _logger;

    public OutboxMessageDispatcher(IServiceProvider serviceProvider, ILogger<OutboxMessageDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        // Try to find the type in the domain assembly (where events typically live)
        // or application assembly. We'll search AppDomain.
        var messageType = FindType(message.Type);
        if (messageType == null)
        {
            throw new InvalidOperationException($"Type not found for message type '{message.Type}'");
        }

        var payload = JsonSerializer.Deserialize(message.Payload, messageType);
        if (payload == null)
        {
            throw new InvalidOperationException($"Failed to deserialize payload for message '{message.Id}' of type '{message.Type}'");
        }

        var handlerType = typeof(IOutboxMessageHandler<>).MakeGenericType(messageType);
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for type '{handlerType.Name}'");
        }

        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
        {
            throw new InvalidOperationException($"HandleAsync method not found on '{handlerType.Name}'");
        }

        var task = (Task?)method.Invoke(handler, new[] { payload, ct });
        if (task != null)
        {
            await task.ConfigureAwait(false);
        }
    }

    private static Type? FindType(string typeName)
    {
        // Typically, events might be in CFCHub.Application or Domain.
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!assembly.FullName!.StartsWith("CFCHub"))
                continue;

            foreach (var type in assembly.GetTypes())
            {
                if (type.Name == typeName)
                {
                    return type;
                }
            }
        }
        return null;
    }
}
