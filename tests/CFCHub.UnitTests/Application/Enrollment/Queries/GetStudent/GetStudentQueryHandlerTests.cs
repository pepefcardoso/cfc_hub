using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Enrollment.Queries.GetStudent;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment.Queries.GetStudent;

public class GetStudentQueryHandlerTests
{
    private readonly IStudentRepository _studentRepository = Substitute.For<IStudentRepository>();
    private readonly IFieldAccessPolicyService _fieldAccessPolicyService = Substitute.For<IFieldAccessPolicyService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ISystemClock _clock = Substitute.For<ISystemClock>();
    private readonly IIdGenerator _idGenerator = Substitute.For<IIdGenerator>();

    private readonly GetStudentQueryHandler _sut;

    public GetStudentQueryHandlerTests()
    {
        _sut = new GetStudentQueryHandler(
            _studentRepository,
            _fieldAccessPolicyService,
            _currentUserService);
    }

    [Fact]
    public async Task GetStudent_AsReceptionist_CpfIsNull()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var query = new GetStudentQuery(studentId);
        
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

        _studentRepository.GetByIdAsync(Arg.Any<StudentId>(), Arg.Any<CancellationToken>())
            .Returns(student);

        _currentUserService.Role.Returns(RoleType.Receptionist);
        
        _fieldAccessPolicyService.CheckAccess(RoleType.Receptionist, nameof(student.Cpf))
            .Returns(FieldAccess.Denied);
            
        _fieldAccessPolicyService.CheckAccess(RoleType.Receptionist, Arg.Is<string>(s => s != nameof(student.Cpf)))
            .Returns(FieldAccess.Allowed);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Cpf.Should().BeNull();
        result.Name.Should().Be("Test Student");
    }
}
