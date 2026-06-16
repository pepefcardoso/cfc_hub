using System;

namespace CFCHub.Application.Compliance.Queries.GetCnhStatus;

public record CnhStatusResult(bool IsAvailable, string? Status, DateOnly? ExpiryDate, int? Points);
