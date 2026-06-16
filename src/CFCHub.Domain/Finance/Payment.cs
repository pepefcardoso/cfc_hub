using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance.Events;

namespace CFCHub.Domain.Finance;

public sealed class Payment : AggregateRoot<PaymentId>, IAuditable
{
    public StudentId StudentId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public PaymentMethod Method { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public string? ReceiptS3Key { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    private Payment(
        PaymentId id,
        StudentId studentId,
        EnrollmentId enrollmentId,
        Money amount,
        PaymentMethod method) : base(id)
    {
        StudentId = studentId;
        EnrollmentId = enrollmentId;
        Amount = amount;
        Method = method;
        Status = PaymentStatus.Pending;
    }

#pragma warning disable CS8618
    private Payment() { }
#pragma warning restore CS8618

    public static Payment Create(
        PaymentId id,
        StudentId studentId,
        EnrollmentId enrollmentId,
        Money amount,
        PaymentMethod method)
    {
        return new Payment(id, studentId, enrollmentId, amount, method);
    }

    public void Confirm(ISystemClock clock)
    {
        if (Status == PaymentStatus.Confirmed)
        {
            return;
        }

        Status = PaymentStatus.Confirmed;
        PaidAt = clock.UtcNow;

        AddDomainEvent(new PaymentReceivedEvent(Id, clock.UtcNow));
    }

    public void Refund(string reason)
    {
        if (Status != PaymentStatus.Confirmed)
        {
            throw new UnprocessableException($"Payment can only be refunded if it is Confirmed. Current status: {Status}");
        }

        Status = PaymentStatus.Refunded;
    }
}
