using System;
using System.Reflection;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Enrollment;

namespace CFCHub.UnitTests.Builders;

public class ContractBuilder
{
    private ContractId _id = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    private StudentId _studentId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private EnrollmentId _enrollmentId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private string? _templateKey = "template.pdf";
    private ContractStatus _status = ContractStatus.Pending;

    public ContractBuilder WithId(ContractId id)
    {
        _id = id;
        return this;
    }

    public ContractBuilder WithStatus(ContractStatus status)
    {
        _status = status;
        return this;
    }

    public Contract Build()
    {
        var ctor = typeof(Contract).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] 
        { 
            typeof(ContractId), typeof(StudentId), typeof(EnrollmentId), typeof(string)
        }, null);
        
        if (ctor == null)
            throw new InvalidOperationException("Contract constructor not found.");

        var contract = (Contract)ctor.Invoke(new object?[] { _id, _studentId, _enrollmentId, _templateKey });
        
        typeof(Contract).GetProperty(nameof(Contract.Status))!.SetValue(contract, _status);
        
        return contract;
    }
}
