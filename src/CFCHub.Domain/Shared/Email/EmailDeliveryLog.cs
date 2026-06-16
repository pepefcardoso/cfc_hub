using System;

namespace CFCHub.Domain.Shared.Email;

public class EmailDeliveryLog
{
    public EmailDeliveryLogId Id { get; private set; }
    public string SesMessageId { get; private set; }
    public string EventType { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public string RecipientAddressHash { get; private set; }
    public string? BounceType { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private EmailDeliveryLog()
    {
        Id = null!;
        SesMessageId = null!;
        EventType = null!;
        RecipientAddressHash = null!;
    }

    public static EmailDeliveryLog Create(
        EmailDeliveryLogId id,
        string sesMessageId,
        string eventType,
        string recipientAddressHash,
        DateTimeOffset occurredAt,
        string? bounceType = null)
    {
        return new EmailDeliveryLog
        {
            Id = id,
            SesMessageId = sesMessageId,
            EventType = eventType,
            RecipientAddressHash = recipientAddressHash,
            OccurredAt = occurredAt,
            BounceType = bounceType,
            Timestamp = DateTimeOffset.UtcNow
        };
    }
}
