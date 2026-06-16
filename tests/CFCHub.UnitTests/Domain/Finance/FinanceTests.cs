using System;
using System.Linq;
using Xunit;
using FluentAssertions;
using NSubstitute;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Finance.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Enrollment;

namespace CFCHub.UnitTests.Domain.Finance;

public class FinanceTests
{
    [Fact]
    public void Money_NegativeAmount_ThrowsUnprocessable()
    {
        // Act
        Action act = () => new Money(-10.50m);

        // Assert
        act.Should().Throw<UnprocessableException>();
    }

    [Fact]
    public void Payment_Refund_WhenPending_ThrowsUnprocessable()
    {
        // Arrange
        var payment = Payment.Create(
            new PaymentId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            new EnrollmentId(Guid.NewGuid()),
            new Money(100m),
            PaymentMethod.CreditCard);

        // Act
        Action act = () => payment.Refund("Customer request");

        // Assert
        act.Should().Throw<UnprocessableException>()
           .WithMessage("Payment can only be refunded if it is Confirmed*");
    }

    [Fact]
    public void Payment_Confirm_RaisesPaymentReceived()
    {
        // Arrange
        var payment = Payment.Create(
            new PaymentId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            new EnrollmentId(Guid.NewGuid()),
            new Money(100m),
            PaymentMethod.CreditCard);
            
        var clock = Substitute.For<ISystemClock>();
        var now = DateTimeOffset.UtcNow;
        clock.UtcNow.Returns(now);

        // Act
        payment.Confirm(clock);

        // Assert
        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.DomainEvents.Should().ContainSingle(e => e is PaymentReceivedEvent);
        var domainEvent = (PaymentReceivedEvent)payment.DomainEvents.Single();
        domainEvent.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void Installment_MarkOverdue_RaisesInvoiceOverdueEvent()
    {
        // Arrange
        var installment = Installment.Create(
            new InstallmentId(Guid.NewGuid()),
            new EnrollmentId(Guid.NewGuid()),
            new Money(500m),
            new DateOnly(2023, 10, 1)
        );
        
        var clock = Substitute.For<ISystemClock>();
        var now = DateTimeOffset.UtcNow;
        clock.UtcNow.Returns(now);

        // Act
        installment.MarkOverdue(clock);

        // Assert
        installment.Status.Should().Be(InstallmentStatus.Overdue);
        installment.DomainEvents.Should().ContainSingle(e => e is InvoiceOverdueEvent);
        var domainEvent = (InvoiceOverdueEvent)installment.DomainEvents.Single();
        domainEvent.OccurredAt.Should().Be(now);
    }
}
