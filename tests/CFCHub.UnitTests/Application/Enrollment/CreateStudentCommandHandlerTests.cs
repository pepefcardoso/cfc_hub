using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Enrollment.Commands.CreateStudent;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment;

public class CreateStudentCommandHandlerTests
{
    [Fact]
    public void CreateStudent_WithInvalidCpfAlgorithm_ThrowsValidation()
    {
        // Arrange
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        var validator = new CreateStudentCommandValidator(clock);
        
        var command = new CreateStudentCommand(
            "John Doe",
            "11111111111", // Invalid algorithm
            "1234567",
            "john@example.com",
            "+5511999999999",
            new DateOnly(1990, 1, 1),
            new AddressRequest("St", "1", null, "Dist", "City", "SP", "12345678"),
            "v1",
            "hash",
            ConsentChannel.Web
        );

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Invalid CPF algorithm.");
    }

    [Fact]
    public async Task CreateStudent_WithDuplicateCpf_ThrowsConflict()
    {
        // Arrange
        var studentRepository = Substitute.For<IStudentRepository>();
        var consentRecordRepository = Substitute.For<IConsentRecordRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<ISystemClock>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var currentUserService = Substitute.For<ICurrentUserService>();

        var handler = new CreateStudentCommandHandler(
            studentRepository,
            consentRecordRepository,
            unitOfWork,
            clock,
            idGenerator,
            currentUserService);

        var command = new CreateStudentCommand(
            "John Doe",
            "12345678909",
            "1234567",
            "john@example.com",
            "+5511999999999",
            new DateOnly(1990, 1, 1),
            new AddressRequest("St", "1", null, "Dist", "City", "SP", "12345678"),
            "v1",
            "hash",
            ConsentChannel.Web
        );

        var existingStudent = new StudentBuilder().WithCpf("12345678909").Build();
        studentRepository.GetByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(existingStudent));

        // Act & Assert
        var action = () => handler.Handle(command, CancellationToken.None);
        await action.Should().ThrowAsync<ConflictException>().WithMessage("STUDENT_ALREADY_EXISTS");
    }

    [Fact]
    public async Task CreateStudent_CreatesConsentRecordInSameUnitOfWork()
    {
        // Arrange
        var studentRepository = Substitute.For<IStudentRepository>();
        var consentRecordRepository = Substitute.For<IConsentRecordRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clock = Substitute.For<ISystemClock>();
        var idGenerator = Substitute.For<IIdGenerator>();
        var currentUserService = Substitute.For<ICurrentUserService>();

        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        idGenerator.NewId<StudentId>().Returns(new StudentId(Guid.NewGuid()));
        idGenerator.NewId<ConsentRecordId>().Returns(new ConsentRecordId(Guid.NewGuid()));
        currentUserService.IpAddress.Returns("127.0.0.1");
        currentUserService.UserAgent.Returns("TestAgent");

        var handler = new CreateStudentCommandHandler(
            studentRepository,
            consentRecordRepository,
            unitOfWork,
            clock,
            idGenerator,
            currentUserService);

        var command = new CreateStudentCommand(
            "John Doe",
            "12345678909",
            "1234567",
            "john@example.com",
            "+5511999999999",
            new DateOnly(1990, 1, 1),
            new AddressRequest("St", "1", null, "Dist", "City", "SP", "12345678"),
            "v1",
            "hash",
            ConsentChannel.Web
        );

        studentRepository.GetByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(null));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            studentRepository.AddAsync(Arg.Any<Student>(), Arg.Any<CancellationToken>());
            consentRecordRepository.AddAsync(Arg.Any<ConsentRecord>(), Arg.Any<CancellationToken>());
            unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }
}
