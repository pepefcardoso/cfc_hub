using System;

namespace CFCHub.Domain.Shared;

public readonly record struct StronglyTypedId<TValue>(TValue Value)
{
    public override string ToString() => Value?.ToString() ?? string.Empty;

    public static implicit operator TValue(StronglyTypedId<TValue> stronglyTypedId) => stronglyTypedId.Value;
}
