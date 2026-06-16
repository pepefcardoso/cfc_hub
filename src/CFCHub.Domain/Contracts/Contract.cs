using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Contracts.Events;

namespace CFCHub.Domain.Contracts;

public class Contract : AggregateRoot<ContractId>, IAuditable
{
    public StudentId StudentId { get; private set; }
    public EnrollmentId EnrollmentId { get; private set; }
    public ContractStatus Status { get; private set; }
    public string? S3Key { get; private set; }
    public DateTimeOffset? SignedAt { get; private set; }
    public string? TemplateKey { get; private set; }
    public SignatureRecord? Signature { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Contract(ContractId id, StudentId studentId, EnrollmentId enrollmentId, string? templateKey) : base(id)
    {
        StudentId = studentId;
        EnrollmentId = enrollmentId;
        TemplateKey = templateKey;
        Status = ContractStatus.Pending;
    }

    private Contract() : base()
    {
        StudentId = null!;
        EnrollmentId = null!;
    } // EF Core

    public static Contract Create(ContractId id, StudentId studentId, EnrollmentId enrollmentId, string? templateKey, ISystemClock clock)
    {
        var contract = new Contract(id, studentId, enrollmentId, templateKey);
        contract.AddDomainEvent(new ContractGenerationRequestedEvent(id, clock.UtcNow));
        return contract;
    }

    public void MarkGenerated(string s3Key)
    {
        S3Key = s3Key;
        Status = ContractStatus.Generated;
    }

    public void Sign(SignatureRecord signature, ISystemClock clock)
    {
        if (Status == ContractStatus.Pending)
        {
            throw new UnprocessableException("Contract cannot be signed while pending generation.", "CONTRACT_PENDING");
        }
        
        if (Status != ContractStatus.Generated)
        {
            throw new UnprocessableException($"Contract in status {Status} cannot be signed.", "CONTRACT_NOT_GENERATED");
        }

        Signature = signature;
        Status = ContractStatus.Signed;
        SignedAt = clock.UtcNow;

        AddDomainEvent(new ContractSignedEvent(Id, clock.UtcNow));
    }
}
