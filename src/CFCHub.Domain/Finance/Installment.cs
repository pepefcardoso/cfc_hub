using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance.Events;

namespace CFCHub.Domain.Finance;

public sealed class Installment : Entity<InstallmentId>
{
    public EnrollmentId EnrollmentId { get; private set; }
    public Money Amount { get; private set; }
    public DateOnly DueDate { get; private set; }
    public InstallmentStatus Status { get; private set; }

    private Installment(
        InstallmentId id,
        EnrollmentId enrollmentId,
        Money amount,
        DateOnly dueDate) : base(id)
    {
        EnrollmentId = enrollmentId;
        Amount = amount;
        DueDate = dueDate;
        Status = InstallmentStatus.Pending;
    }

#pragma warning disable CS8618
    private Installment() { }
#pragma warning restore CS8618

    public static Installment Create(
        InstallmentId id,
        EnrollmentId enrollmentId,
        Money amount,
        DateOnly dueDate)
    {
        return new Installment(id, enrollmentId, amount, dueDate);
    }

    public void MarkPaid(PaymentId paymentId)
    {
        if (Status == InstallmentStatus.Paid)
        {
            return;
        }

        Status = InstallmentStatus.Paid;
    }

    public void MarkOverdue(ISystemClock clock)
    {
        if (Status == InstallmentStatus.Paid)
        {
            throw new UnprocessableException("Cannot mark a paid installment as overdue.");
        }

        Status = InstallmentStatus.Overdue;
        AddDomainEvent(new InvoiceOverdueEvent(Id, clock.UtcNow));
    }
}
