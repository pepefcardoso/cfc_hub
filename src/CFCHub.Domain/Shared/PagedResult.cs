using System.Collections.Generic;

namespace CFCHub.Domain.Shared;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore,
    int Count);
