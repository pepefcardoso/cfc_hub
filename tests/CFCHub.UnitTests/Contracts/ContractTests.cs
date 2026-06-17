using System;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Contracts.Events;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Contracts;

public class ContractTests
{
    private readonly ISystemClock _clock;

    public ContractTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Contract_Create_ReturnsPendingContractAndRaisesEvent()
    {
        var id = new ContractId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());
        var enrollmentId = new EnrollmentId(Guid.NewGuid());

        var contract = Contract.Create(id, studentId, enrollmentId, "template", _clock);

        contract.Status.Should().Be(ContractStatus.Pending);
        contract.DomainEvents.Should().ContainSingle(e => e is ContractGenerationRequestedEvent);
    }

    [Fact]
    public void Contract_MarkGenerated_SetsS3KeyAndStatus()
    {
        var contract = new ContractBuilder().WithStatus(ContractStatus.Pending).Build();

        contract.MarkGenerated("s3/key.pdf");

        contract.S3Key.Should().Be("s3/key.pdf");
        contract.Status.Should().Be(ContractStatus.Generated);
    }

    [Fact]
    public void Contract_Sign_WhenPending_ThrowsUnprocessable()
    {
        var contract = new ContractBuilder().WithStatus(ContractStatus.Pending).Build();
        var signature = new SignatureRecord(new SignatureRecordId(Guid.NewGuid()), contract.Id, "hash", "127.0.0.1", _clock.UtcNow);

        Action act = () => contract.Sign(signature, _clock);

        act.Should().Throw<UnprocessableException>().WithMessage("*cannot be signed while pending generation*");
    }

    [Fact]
    public void Contract_Sign_WhenStatusIsSigned_ThrowsUnprocessable()
    {
        var contract = new ContractBuilder().WithStatus(ContractStatus.Signed).Build();
        var signature = new SignatureRecord(new SignatureRecordId(Guid.NewGuid()), contract.Id, "hash", "127.0.0.1", _clock.UtcNow);

        Action act = () => contract.Sign(signature, _clock);

        act.Should().Throw<UnprocessableException>().WithMessage("*cannot be signed*");
    }

    [Fact]
    public void Contract_Sign_WhenGenerated_RaisesContractSignedEvent()
    {
        var contract = new ContractBuilder().WithStatus(ContractStatus.Generated).Build();
        var signature = new SignatureRecord(new SignatureRecordId(Guid.NewGuid()), contract.Id, "hash", "127.0.0.1", _clock.UtcNow);

        contract.Sign(signature, _clock);

        contract.Status.Should().Be(ContractStatus.Signed);
        contract.SignedAt.Should().Be(_clock.UtcNow);
        contract.Signature.Should().Be(signature);
        contract.DomainEvents.Should().ContainSingle(e => e is ContractSignedEvent);
    }
}
