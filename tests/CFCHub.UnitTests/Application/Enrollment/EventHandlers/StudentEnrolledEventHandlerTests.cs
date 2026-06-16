using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Enrollment.EventHandlers;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Enrollment.Events;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment.EventHandlers;

public class StudentEnrolledEventHandlerTests
{
    private readonly IContractRepository _contractRepository;
    private readonly IIdGenerator _idGenerator;
    private readonly ISystemClock _clock;
    private readonly StudentEnrolledEventHandler _handler;

    public StudentEnrolledEventHandlerTests()
    {
        _contractRepository = Substitute.For<IContractRepository>();
        _idGenerator = Substitute.For<IIdGenerator>();
        _clock = Substitute.For<ISystemClock>();

        _handler = new StudentEnrolledEventHandler(_contractRepository, _idGenerator, _clock);
    }

    [Fact]
    public async Task StudentEnrolled_CreatesContract()
    {
        // Arrange
        var enrollmentId = new EnrollmentId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());
        var contractId = new ContractId(Guid.NewGuid());
        var utcNow = DateTimeOffset.UtcNow;

        _idGenerator.NewId<ContractId>().Returns(contractId);
        _clock.UtcNow.Returns(utcNow);

        var notification = new StudentEnrolledEvent(
            enrollmentId,
            studentId,
            CnhCategory.B,
            utcNow);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _contractRepository.Received(1).AddAsync(Arg.Is<Contract>(c =>
            c.Id.Value == contractId &&
            c.StudentId == studentId &&
            c.EnrollmentId == enrollmentId &&
            c.Status == ContractStatus.Pending
        ), Arg.Any<CancellationToken>());
    }
}
