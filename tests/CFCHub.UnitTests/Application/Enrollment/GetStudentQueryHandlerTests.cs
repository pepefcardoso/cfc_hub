using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Enrollment.Queries.GetStudent;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Identity;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Enrollment;

public class GetStudentQueryHandlerTests
{
    [Fact]
    public async Task GetStudent_AsReceptionist_CpfIsNull()
    {
        // Arrange
        var studentRepository = Substitute.For<IStudentRepository>();
        var fieldAccessPolicyService = Substitute.For<IFieldAccessPolicyService>();
        var currentUserService = Substitute.For<ICurrentUserService>();

        var handler = new GetStudentQueryHandler(studentRepository, fieldAccessPolicyService, currentUserService);

        var student = new StudentBuilder().WithCpf("12345678909").Build();
        studentRepository.GetByIdAsync(student.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(student));

        currentUserService.Role.Returns(RoleType.Receptionist);

        fieldAccessPolicyService.CheckAccess(RoleType.Receptionist, nameof(student.Cpf))
            .Returns(FieldAccess.Denied);
        
        fieldAccessPolicyService.CheckAccess(RoleType.Receptionist, nameof(student.Name))
            .Returns(FieldAccess.Allowed);

        var query = new GetStudentQuery(student.Id.Value);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Id.Should().Be(student.Id.Value);
        result.Name.Should().Be(student.Name);
        result.Cpf.Should().BeNull();
    }

    [Fact]
    public async Task GetStudent_AsAdmin_CpfIsPresent()
    {
        // Arrange
        var studentRepository = Substitute.For<IStudentRepository>();
        var fieldAccessPolicyService = Substitute.For<IFieldAccessPolicyService>();
        var currentUserService = Substitute.For<ICurrentUserService>();

        var handler = new GetStudentQueryHandler(studentRepository, fieldAccessPolicyService, currentUserService);

        var student = new StudentBuilder().WithCpf("12345678909").Build();
        studentRepository.GetByIdAsync(student.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Student?>(student));

        currentUserService.Role.Returns(RoleType.Admin);

        fieldAccessPolicyService.CheckAccess(RoleType.Admin, nameof(student.Cpf))
            .Returns(FieldAccess.Allowed);
            
        fieldAccessPolicyService.CheckAccess(RoleType.Admin, nameof(student.Name))
            .Returns(FieldAccess.Allowed);

        var query = new GetStudentQuery(student.Id.Value);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Id.Should().Be(student.Id.Value);
        result.Name.Should().Be(student.Name);
        result.Cpf.Should().Be("12345678909");
    }
}
