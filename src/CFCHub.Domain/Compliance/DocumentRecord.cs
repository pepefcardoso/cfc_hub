using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Students;
using CFCHub.Domain.Compliance.Events;

namespace CFCHub.Domain.Compliance;

public sealed class DocumentRecord : AggregateRoot<DocumentRecordId>, IAuditable
{
    public StudentId StudentId { get; private set; }
    public DocumentType Type { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public string? S3Key { get; private set; }
    public DateTimeOffset? LastAlertSentAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private DocumentRecord() 
    {
        StudentId = null!;
    } // EF Core

    private DocumentRecord(
        DocumentRecordId id,
        StudentId studentId,
        DocumentType type,
        DateOnly expiryDate,
        string? s3Key) : base(id)
    {
        StudentId = studentId;
        Type = type;
        ExpiryDate = expiryDate;
        S3Key = s3Key;
    }

    public static DocumentRecord Create(
        StudentId studentId,
        DocumentType type,
        DateOnly expiryDate,
        IIdGenerator idGenerator,
        string? s3Key = null)
    {
        return new DocumentRecord(
            idGenerator.NewId<DocumentRecordId>(),
            studentId,
            type,
            expiryDate,
            s3Key);
    }

    public void MarkAlertSent(AlertTier tier, ISystemClock clock)
    {
        var now = clock.UtcNow;

        if (LastAlertSentAt.HasValue && (now - LastAlertSentAt.Value).TotalHours < 24)
        {
            throw new UnprocessableException("ALERT_ALREADY_SENT_TODAY");
        }

        LastAlertSentAt = now;

        AddDomainEvent(new DocumentExpiryAlertRequestedEvent(
            Id,
            StudentId.Value,
            Type,
            tier,
            now));
    }
}
