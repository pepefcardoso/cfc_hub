using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Outbox;
using CFCHub.Infrastructure.Persistence;

namespace CFCHub.Infrastructure.Outbox;

public class OutboxService : IOutboxService
{
    private readonly AppDbContext _context;
    private readonly ISystemClock _clock;

    public OutboxService(AppDbContext context, ISystemClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task InsertAsync(string type, string payload, CancellationToken cancellationToken = default)
    {
        var outboxMessage = OutboxMessage.Create(type, payload, _clock.UtcNow);
        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
