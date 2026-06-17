using System;

namespace CFCHub.Domain.Shared.Outbox;

public readonly record struct OutboxMessageId(Guid Value)
{
    public static OutboxMessageId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
