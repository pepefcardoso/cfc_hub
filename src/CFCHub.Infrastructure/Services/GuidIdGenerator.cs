using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Infrastructure.Services;

public class GuidIdGenerator : IIdGenerator
{
    public TId NewId<TId>() where TId : StronglyTypedId<Guid>
    {
        return (TId)Activator.CreateInstance(typeof(TId), Guid.NewGuid())!;
    }
}
