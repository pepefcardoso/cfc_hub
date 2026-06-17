using System;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Finance.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Finance;

public class InstallmentTests
{
    private readonly ISystemClock _clock;

    public InstallmentTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Installment_Create_ReturnsPendingInstallment()
    {
        var id = new InstallmentId(Guid.NewGuid());
        var enrollmentId = new EnrollmentId(Guid.NewGuid());
        var amount = new Money(100.0m);
        var dueDate = new DateOnly(2026, 2, 1);

        var installment = Installment.Create(id, enrollmentId, amount, dueDate);

        installment.EnrollmentId.Should().Be(enrollmentId);
        installment.Amount.Should().Be(amount);
        installment.DueDate.Should().Be(dueDate);
        installment.Status.Should().Be(InstallmentStatus.Pending);
    }

    [Fact]
    public void Installment_MarkPaid_SetsPaidStatus()
    {
        var installment = Installment.Create(new InstallmentId(Guid.NewGuid()), new EnrollmentId(Guid.NewGuid()), new Money(100.0m), new DateOnly(2026, 2, 1));
        var paymentId = new PaymentId(Guid.NewGuid());

        installment.MarkPaid(paymentId);

        installment.Status.Should().Be(InstallmentStatus.Paid);
    }

    [Fact]
    public void Installment_MarkPaid_WhenAlreadyPaid_DoesNothing()
    {
        var installment = Installment.Create(new InstallmentId(Guid.NewGuid()), new EnrollmentId(Guid.NewGuid()), new Money(100.0m), new DateOnly(2026, 2, 1));
        var paymentId = new PaymentId(Guid.NewGuid());

        installment.MarkPaid(paymentId);
        installment.MarkPaid(paymentId);

        installment.Status.Should().Be(InstallmentStatus.Paid);
    }

    [Fact]
    public void Installment_MarkOverdue_SetsOverdueStatus()
    {
        var installment = Installment.Create(new InstallmentId(Guid.NewGuid()), new EnrollmentId(Guid.NewGuid()), new Money(100.0m), new DateOnly(2026, 2, 1));

        installment.MarkOverdue(_clock);

        installment.Status.Should().Be(InstallmentStatus.Overdue);
        installment.DomainEvents.Should().ContainSingle(e => e is InvoiceOverdueEvent);
    }

    [Fact]
    public void Installment_MarkOverdue_WhenPaid_ThrowsUnprocessable()
    {
        var installment = Installment.Create(new InstallmentId(Guid.NewGuid()), new EnrollmentId(Guid.NewGuid()), new Money(100.0m), new DateOnly(2026, 2, 1));
        installment.MarkPaid(new PaymentId(Guid.NewGuid()));

        Action act = () => installment.MarkOverdue(_clock);

        act.Should().Throw<UnprocessableException>().WithMessage("*Cannot mark a paid installment as overdue*");
    }

    [Fact]
    public void Installment_EfCoreConstructor_Exists()
    {
        var instance = Activator.CreateInstance(typeof(Installment), true);
        instance.Should().NotBeNull();
    }
}
