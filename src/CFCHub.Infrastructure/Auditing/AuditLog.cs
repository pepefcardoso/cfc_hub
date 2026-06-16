namespace CFCHub.Infrastructure.Auditing;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? UserId { get; set; }
}
