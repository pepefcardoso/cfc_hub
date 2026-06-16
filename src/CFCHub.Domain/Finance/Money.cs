using System;
using System.Collections.Generic;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;

namespace CFCHub.Domain.Finance;

public class Money : ValueObject, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "BRL")
    {
        if (amount < 0)
        {
            throw new UnprocessableException("Money amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must be specified", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private Money() { }
#pragma warning restore CS8618

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies.");
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies.");
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies.");
        return Amount.CompareTo(other.Amount);
    }

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
}
