using System;

namespace CFCHub.Domain.Shared;

public interface IIdGenerator
{
    TId NewId<TId>() where TId : StronglyTypedId<Guid>;
}
