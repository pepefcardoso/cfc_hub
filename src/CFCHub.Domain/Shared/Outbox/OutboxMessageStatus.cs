namespace CFCHub.Domain.Shared.Outbox;

public enum OutboxMessageStatus
{
    Pending,
    Processing,
    Processed,
    Failed
}
