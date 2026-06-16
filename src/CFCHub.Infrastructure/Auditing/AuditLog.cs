namespace CFCHub.Infrastructure.Auditing;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangedFields { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string TraceId { get; set; } = string.Empty;
}
