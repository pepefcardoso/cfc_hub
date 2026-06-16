using System;

namespace CFCHub.Application.Contracts.Queries.GetContract;

public record ContractResult(
    Guid Id,
    Guid StudentId,
    Guid EnrollmentId,
    string Status,
    string? DownloadUrl,
    DateTimeOffset? SignedAt,
    DateTimeOffset CreatedAt);
