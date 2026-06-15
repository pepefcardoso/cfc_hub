using System;

namespace CFCHub.Domain.Shared;

public abstract record StronglyTypedId<TValue>(TValue Value)
{
    public sealed override string ToString() => Value?.ToString() ?? string.Empty;

    public static implicit operator TValue(StronglyTypedId<TValue> stronglyTypedId) => stronglyTypedId.Value;
}
