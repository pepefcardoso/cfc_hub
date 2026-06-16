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
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment.Commands.EnrollStudent;

public class EnrollStudentCommandHandlerTests
{
    private readonly IStudentRepository _studentRepository = Substitute.For<IStudentRepository>();
    private readonly IEnrollmentRepository _enrollmentRepository = Substitute.For<IEnrollmentRepository>();
    private readonly IOutboxService _outboxService = Substitute.For<IOutboxService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private readonly IIdGenerator _idGenerator = Substitute.For<IIdGenerator>();

    private readonly EnrollStudentCommandHandler _sut;

    public EnrollStudentCommandHandlerTests()
    {
        _sut = new EnrollStudentCommandHandler(
            _studentRepository,
            _enrollmentRepository,
            _outboxService,
            _unitOfWork,
            _clock,
            _idGenerator);
    }

    [Fact]
    public async Task EnrollStudent_WithExistingEnrollmentInSameCategory_ThrowsConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var command = new EnrollStudentCommand(studentId, CnhCategory.B);
        
        var student = Student.Create(
            new StudentId(studentId),
            "Test Student",
            "12345678901",
            "1234567",
            "test@test.com",
            "11999999999",
            new DateOnly(2000, 1, 1),
            Address.Empty,
            _clock,
            _idGenerator);

        var existingEnrollment = CFCHub.Domain.Enrollment.Enrollment.Enroll(
            new EnrollmentId(Guid.NewGuid()),
            new StudentId(studentId),
            CnhCategory.B,
            _clock);

        _studentRepository.GetByIdAsync(Arg.Any<StudentId>(), Arg.Any<CancellationToken>())
            .Returns(student);

        _enrollmentRepository.GetByStudentIdAsync(Arg.Any<StudentId>(), Arg.Any<CancellationToken>())
            .Returns(new List<CFCHub.Domain.Enrollment.Enrollment> { existingEnrollment });

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("ENROLLMENT_ALREADY_EXISTS");
    }
}
