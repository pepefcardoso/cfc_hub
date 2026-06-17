using System;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Finance;

public class MoneyTests
{
    [Fact]
    public void Money_NegativeAmount_ThrowsUnprocessable()
    {
        Action act = () => new Money(-10.0m);
        act.Should().Throw<UnprocessableException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public void Money_EmptyCurrency_ThrowsArgumentException()
    {
        Action act = () => new Money(10.0m, "");
        act.Should().Throw<ArgumentException>().WithMessage("*Currency must be specified*");
    }

    [Fact]
    public void Money_Addition_SameCurrency_ReturnsSum()
    {
        var m1 = new Money(10.0m, "BRL");
        var m2 = new Money(20.0m, "BRL");

        var m3 = m1 + m2;

        m3.Amount.Should().Be(30.0m);
        m3.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Money_Addition_DifferentCurrency_ThrowsInvalidOperation()
    {
        var m1 = new Money(10.0m, "BRL");
        var m2 = new Money(20.0m, "USD");

        Action act = () => { var res = m1 + m2; };

        act.Should().Throw<InvalidOperationException>().WithMessage("*different currencies*");
    }

    [Fact]
    public void Money_Subtraction_SameCurrency_ReturnsDifference()
    {
        var m1 = new Money(20.0m, "BRL");
        var m2 = new Money(5.0m, "BRL");

        var m3 = m1 - m2;

        m3.Amount.Should().Be(15.0m);
        m3.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Money_Subtraction_DifferentCurrency_ThrowsInvalidOperation()
    {
        var m1 = new Money(20.0m, "BRL");
        var m2 = new Money(5.0m, "USD");

        Action act = () => { var res = m1 - m2; };

        act.Should().Throw<InvalidOperationException>().WithMessage("*different currencies*");
    }

    [Fact]
    public void Money_Comparison_SameCurrency_Works()
    {
        var m1 = new Money(10.0m, "BRL");
        var m2 = new Money(20.0m, "BRL");

        (m1 < m2).Should().BeTrue();
        (m2 > m1).Should().BeTrue();
        var m1_copy = new Money(10.0m, "BRL");
        (m1 <= m1_copy).Should().BeTrue();
        (m1 >= m1_copy).Should().BeTrue();
        m1.CompareTo(null).Should().Be(1);
    }

    [Fact]
    public void Money_Comparison_DifferentCurrency_ThrowsInvalidOperation()
    {
        var m1 = new Money(10.0m, "BRL");
        var m2 = new Money(20.0m, "USD");

        Action act = () => m1.CompareTo(m2);

        act.Should().Throw<InvalidOperationException>().WithMessage("*different currencies*");
    }

    [Fact]
    public void Money_Equality_Works()
    {
        var m1 = new Money(10.0m, "BRL");
        var m2 = new Money(10.0m, "BRL");
        var m3 = new Money(10.0m, "USD");
        var m4 = new Money(20.0m, "BRL");

        m1.Should().Be(m2);
        m1.Should().NotBe(m3);
        m1.Should().NotBe(m4);
    }
}
