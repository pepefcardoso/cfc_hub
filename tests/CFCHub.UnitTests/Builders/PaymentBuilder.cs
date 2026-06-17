using System;
using System.Reflection;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Enrollment;

namespace CFCHub.UnitTests.Builders;

public class PaymentBuilder
{
    private PaymentId _id = new(Guid.Parse("44444444-4444-4444-4444-444444444444"));
    private StudentId _studentId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private EnrollmentId _enrollmentId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private Money _amount = new(100.0m);
    private PaymentMethod _method = PaymentMethod.Pix;
    private PaymentStatus _status = PaymentStatus.Pending;

    public PaymentBuilder WithId(PaymentId id)
    {
        _id = id;
        return this;
    }

    public PaymentBuilder WithStatus(PaymentStatus status)
    {
        _status = status;
        return this;
    }

    public Payment Build()
    {
        var ctor = typeof(Payment).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] 
        { 
            typeof(PaymentId), typeof(StudentId), typeof(EnrollmentId), typeof(Money), typeof(PaymentMethod)
        }, null);
        
        if (ctor == null)
            throw new InvalidOperationException("Payment constructor not found.");

        var payment = (Payment)ctor.Invoke(new object?[] { _id, _studentId, _enrollmentId, _amount, _method });
        
        typeof(Payment).GetProperty(nameof(Payment.Status))!.SetValue(payment, _status);
        
        return payment;
    }
}
