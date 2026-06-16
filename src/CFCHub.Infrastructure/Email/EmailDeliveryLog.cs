using System;

namespace CFCHub.Infrastructure.Email;

public class EmailDeliveryLog
{
    public Guid Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public string DestinationAddress { get; set; } = string.Empty;
    public string? StatusDetails { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
