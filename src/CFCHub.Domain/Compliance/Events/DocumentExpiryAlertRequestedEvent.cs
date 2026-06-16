using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Compliance.Events;

public sealed record DocumentExpiryAlertRequestedEvent(
    DocumentRecordId DocumentRecordId,
    Guid StudentId,
    DocumentType DocumentType,
    AlertTier Tier,
    DateTimeOffset RequestedAt
) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = RequestedAt;
}
