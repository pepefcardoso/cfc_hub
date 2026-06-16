using System;
using CFCHub.Domain.Compliance;

namespace CFCHub.Application.Compliance.Queries.GetExpiringDocuments;

public record ExpiringDocumentResult(
    Guid Id,
    Guid StudentId,
    DocumentType Type,
    DateOnly ExpiryDate,
    DateTimeOffset? LastAlertSentAt
);
