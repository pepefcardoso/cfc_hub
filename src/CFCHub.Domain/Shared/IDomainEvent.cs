using System;

namespace CFCHub.Domain.Shared;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
