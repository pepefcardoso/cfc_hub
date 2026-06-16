using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Compliance;

public sealed class DocumentExpiryAlert : Entity<DocumentExpiryAlertId>
{
    public DocumentRecordId DocumentRecordId { get; private set; }
    public AlertTier Tier { get; private set; }
    public DateTimeOffset SentAt { get; private set; }

    private DocumentExpiryAlert() 
    {
        DocumentRecordId = null!;
    } // EF Core

    private DocumentExpiryAlert(DocumentExpiryAlertId id, DocumentRecordId documentRecordId, AlertTier tier, DateTimeOffset sentAt)
        : base(id)
    {
        DocumentRecordId = documentRecordId;
        Tier = tier;
        SentAt = sentAt;
    }

    public static DocumentExpiryAlert Create(DocumentRecordId documentRecordId, AlertTier tier, DateTimeOffset sentAt, IIdGenerator idGenerator)
    {
        return new DocumentExpiryAlert(idGenerator.NewId<DocumentExpiryAlertId>(), documentRecordId, tier, sentAt);
    }
}
