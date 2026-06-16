using System;

namespace CFCHub.Domain.Compliance;

public record CnhStatusResult(bool IsAvailable, string? Status, DateOnly? ExpiryDate, int? Points)
{
    public static CnhStatusResult Unavailable { get; } = new(false, null, null, null);
}
