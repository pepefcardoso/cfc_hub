using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Enrollment.Commands.CreateStudent;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment.Commands.CreateStudent;

public class CreateStudentCommandHandlerTests
{
    private readonly IStudentRepository _studentRepository;
    private readonly IConsentRecordRepository _consentRecordRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly ICurrentUserService _currentUserService;
    private readonly CreateStudentCommandHandler _handler;

    public CreateStudentCommandHandlerTests()
    {
        _studentRepository = Substitute.For<IStudentRepository>();
        _consentRecordRepository = Substitute.For<IConsentRecordRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _clock = Substitute.For<ISystemClock>();
        _idGenerator = Substitute.For<IIdGenerator>();
        _currentUserService = Substitute.For<ICurrentUserService>();

        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _idGenerator.NewId<StudentId>().Returns(new StudentId(Guid.NewGuid()));
        _idGenerator.NewId<ConsentRecordId>().Returns(new ConsentRecordId(Guid.NewGuid()));
        _currentUserService.IpAddress.Returns("127.0.0.1");

        _handler = new CreateStudentCommandHandler(
            _studentRepository,
            _consentRecordRepository,
            _unitOfWork,
            _clock,
            _idGenerator,
            _currentUserService);
    }

    [Fact]
    public async Task CreateStudent_WithDuplicateCpf_ThrowsConflict()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingStudent = Student.Create(
            new StudentId(Guid.NewGuid()),
            "Existing",
            "12345678909",
            null,
            "existing@test.com",
            "+5511999999999",
            new DateOnly(1990, 1, 1),
            Address.Empty,
            _clock,
            _idGenerator);

        _studentRepository.GetByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(existingStudent));

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("STUDENT_ALREADY_EXISTS");
        await _studentRepository.DidNotReceive().AddAsync(Arg.Any<Student>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateStudent_CreatesConsentRecordAtomically()
    {
        // Arrange
        var command = CreateValidCommand();
        _studentRepository.GetByCpfAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(null));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        
        Received.InOrder(() =>
        {
            _studentRepository.AddAsync(Arg.Any<Student>(), Arg.Any<CancellationToken>());
            _consentRecordRepository.AddAsync(Arg.Any<ConsentRecord>(), Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    private static CreateStudentCommand CreateValidCommand()
    {
        return new CreateStudentCommand(
            "Test Student",
            "12345678909",
            "123456789",
            "test@example.com",
            "+5511999999999",
            new DateOnly(2000, 1, 1),
            new AddressRequest("Street", "123", null, "District", "City", "ST", "12345678"),
            "v1.0",
            "hash123",
            ConsentChannel.Web);
    }
}
