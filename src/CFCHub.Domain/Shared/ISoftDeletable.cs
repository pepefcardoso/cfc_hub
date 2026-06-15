using System;

namespace CFCHub.Domain.Shared;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
    bool IsDeleted => DeletedAt.HasValue;
}
