using System;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Finance.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Finance;

public class PaymentTests
{
    private readonly ISystemClock _clock;

    public PaymentTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Payment_Create_ReturnsPendingPayment()
    {
        var id = new PaymentId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());
        var enrollmentId = new EnrollmentId(Guid.NewGuid());
        var amount = new Money(100.0m);

        var payment = Payment.Create(id, studentId, enrollmentId, amount, PaymentMethod.Pix);

        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.Amount.Should().Be(amount);
    }

    [Fact]
    public void Payment_Confirm_RaisesPaymentReceivedEvent()
    {
        var payment = new PaymentBuilder().WithStatus(PaymentStatus.Pending).Build();

        payment.Confirm(_clock);

        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.PaidAt.Should().Be(_clock.UtcNow);
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentReceivedEvent);
    }

    [Fact]
    public void Payment_Confirm_WhenAlreadyConfirmed_DoesNothing()
    {
        var payment = new PaymentBuilder().WithStatus(PaymentStatus.Confirmed).Build();

        payment.Confirm(_clock);

        payment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Payment_Refund_WhenPending_ThrowsUnprocessable()
    {
        var payment = new PaymentBuilder().WithStatus(PaymentStatus.Pending).Build();

        Action act = () => payment.Refund("Cancelamento");

        act.Should().Throw<UnprocessableException>().WithMessage("*can only be refunded if it is Confirmed*");
    }

    [Fact]
    public void Payment_Refund_WhenConfirmed_SetsRefundedStatus()
    {
        var payment = new PaymentBuilder().WithStatus(PaymentStatus.Confirmed).Build();

        payment.Refund("Cancelamento");

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }
}
