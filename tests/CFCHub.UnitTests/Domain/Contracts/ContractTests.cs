using System;
using System.Linq;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Contracts.Events;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Domain.Contracts;

public class ContractTests
{
    private readonly ISystemClock _clock;

    public ContractTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Create_RaisesContractGenerationRequested()
    {
        // Arrange
        var contractId = new ContractId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());
        var enrollmentId = new EnrollmentId(Guid.NewGuid());
        var templateKey = "template.pdf";

        // Act
        var contract = Contract.Create(contractId, studentId, enrollmentId, templateKey, _clock);

        // Assert
        contract.Status.Should().Be(ContractStatus.Pending);
        contract.StudentId.Should().Be(studentId);
        contract.EnrollmentId.Should().Be(enrollmentId);
        contract.TemplateKey.Should().Be(templateKey);

        var events = contract.DomainEvents.ToList();
        events.Should().ContainSingle();
        var domainEvent = events.First().Should().BeOfType<ContractGenerationRequestedEvent>().Subject;
        domainEvent.ContractId.Should().Be(contractId);
    }

    [Fact]
    public void Sign_WhenPending_ThrowsUnprocessable()
    {
        // Arrange
        var contract = Contract.Create(
            new ContractId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            new EnrollmentId(Guid.NewGuid()),
            "template.pdf",
            _clock);
            
        var signature = new SignatureRecord(
            new SignatureRecordId(Guid.NewGuid()),
            contract.Id,
            "hash",
            "127.0.0.1",
            _clock.UtcNow);

        // Act
        Action act = () => contract.Sign(signature, _clock);

        // Assert
        act.Should().Throw<UnprocessableException>().WithMessage("Contract cannot be signed while pending generation.");
    }

    [Fact]
    public void Sign_WhenGenerated_RaisesContractSigned()
    {
        // Arrange
        var contract = Contract.Create(
            new ContractId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            new EnrollmentId(Guid.NewGuid()),
            "template.pdf",
            _clock);
            
        contract.MarkGenerated("s3_key.pdf");
        
        // Clear events from creation
        contract.ClearDomainEvents();

        var signature = new SignatureRecord(
            new SignatureRecordId(Guid.NewGuid()),
            contract.Id,
            "hash",
            "127.0.0.1",
            _clock.UtcNow);

        // Act
        contract.Sign(signature, _clock);

        // Assert
        contract.Status.Should().Be(ContractStatus.Signed);
        contract.SignedAt.Should().Be(_clock.UtcNow);
        contract.Signature.Should().Be(signature);

        var events = contract.DomainEvents.ToList();
        events.Should().ContainSingle();
        var domainEvent = events.First().Should().BeOfType<ContractSignedEvent>().Subject;
        domainEvent.ContractId.Should().Be(contract.Id);
    }
}
