using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Enrollment.Commands.EnrollStudent;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;
using EnrollmentEntity = CFCHub.Domain.Enrollment.Enrollment;

namespace CFCHub.UnitTests.Application.Enrollment;

public class EnrollStudentCommandHandlerTests
{
    [Fact]
    public async Task EnrollStudent_WithExistingEnrollment_ThrowsConflict()
    {
        // Arrange
        var studentRepository = Substitute.For<IStudentRepository>();
        var enrollmentRepository = Substitute.For<IEnrollmentRepository>();
        var outboxService = Substitute.For<IOutboxService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<ISystemClock>();
        var idGenerator = Substitute.For<IIdGenerator>();

        var handler = new EnrollStudentCommandHandler(
            studentRepository, enrollmentRepository, outboxService, unitOfWork, clock, idGenerator);

        var student = new StudentBuilder().Build();
        studentRepository.GetByIdAsync(student.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(student));

        var existingEnrollment = new EnrollmentBuilder()
            .WithStudentId(student.Id)
            .WithCategory(CnhCategory.B)
            .WithStatus(EnrollmentStatus.Active)
            .Build();

        enrollmentRepository.GetByStudentIdAsync(student.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EnrollmentEntity>>(new[] { existingEnrollment }));

        var command = new EnrollStudentCommand(student.Id.Value, CnhCategory.B);

        // Act & Assert
        var action = () => handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<ConflictException>().WithMessage("ENROLLMENT_ALREADY_EXISTS");
    }

    [Fact]
    public async Task EnrollStudent_PublishesThreeOutboxMessages()
    {
        // Arrange
        var studentRepository = Substitute.For<IStudentRepository>();
        var enrollmentRepository = Substitute.For<IEnrollmentRepository>();
        var outboxService = Substitute.For<IOutboxService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<ISystemClock>();
        var idGenerator = Substitute.For<IIdGenerator>();

        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        idGenerator.NewId<EnrollmentId>().Returns(new EnrollmentId(Guid.NewGuid()));

        var handler = new EnrollStudentCommandHandler(
            studentRepository, enrollmentRepository, outboxService, unitOfWork, clock, idGenerator);

        var student = new StudentBuilder().Build();
        studentRepository.GetByIdAsync(student.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(student));

        enrollmentRepository.GetByStudentIdAsync(student.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<EnrollmentEntity>>(Array.Empty<EnrollmentEntity>()));

        var command = new EnrollStudentCommand(student.Id.Value, CnhCategory.B);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await outboxService.Received(1).InsertAsync("ContractGenerationRequested", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await outboxService.Received(1).InsertAsync("PaymentPlanCreationRequested", Arg.Any<string>(), Arg.Any<CancellationToken>());
        await outboxService.Received(1).InsertAsync("DocumentTrackingRegistered", Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        Received.InOrder(() =>
        {
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
            unitOfWork.CommitTransactionAsync(Arg.Any<CancellationToken>());
        });
    }
}
