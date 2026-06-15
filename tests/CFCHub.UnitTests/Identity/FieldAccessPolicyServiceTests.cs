using CFCHub.Domain.Identity;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Identity;

public class FieldAccessPolicyServiceTests
{
    private readonly FieldAccessPolicyService _sut;

    public FieldAccessPolicyServiceTests()
    {
        _sut = new FieldAccessPolicyService();
    }

    [Fact]
    public void CheckAccess_Receptionist_CannotReadCpf()
    {
        _sut.CheckAccess(RoleType.Receptionist, "Student.Cpf").Should().Be(FieldAccess.Denied);
        _sut.CheckAccess(RoleType.Receptionist, "Cpf").Should().Be(FieldAccess.Denied);
    }

    [Fact]
    public void CheckAccess_Admin_CanReadAllFields()
    {
        _sut.CheckAccess(RoleType.Admin, "Student.Cpf").Should().Be(FieldAccess.Allowed);
        _sut.CheckAccess(RoleType.Admin, "MedicalExam.Result").Should().Be(FieldAccess.Allowed);
        _sut.CheckAccess(RoleType.Admin, "Financial.Invoice").Should().Be(FieldAccess.Allowed);
        _sut.CheckAccess(RoleType.Admin, "Any.Other.Field").Should().Be(FieldAccess.Allowed);
    }

    [Fact]
    public void CheckAccess_Financial_CannotReadMedicalData()
    {
        var result = _sut.CheckAccess(RoleType.Financial, "MedicalExam.History");
        result.Should().Be(FieldAccess.Denied);
    }

    [Fact]
    public void CheckAccess_Receptionist_CanReadAllowedFields()
    {
        _sut.CheckAccess(RoleType.Receptionist, "Student.Name").Should().Be(FieldAccess.Allowed);
        _sut.CheckAccess(RoleType.Receptionist, "Student.Email").Should().Be(FieldAccess.Allowed);
        _sut.CheckAccess(RoleType.Receptionist, "Student.Phone").Should().Be(FieldAccess.Allowed);
    }

    [Fact]
    public void CheckAccess_Instructor_CanReadNameButNotMedicalOrFinancial()
    {
        _sut.CheckAccess(RoleType.Instructor, "Student.Name").Should().Be(FieldAccess.Allowed);
        _sut.CheckAccess(RoleType.Instructor, "MedicalExam.Details").Should().Be(FieldAccess.Denied);
        _sut.CheckAccess(RoleType.Instructor, "Financial.Balance").Should().Be(FieldAccess.Denied);
    }

    [Fact]
    public void CheckAccess_Financial_CanReadFinancialFields()
    {
        _sut.CheckAccess(RoleType.Financial, "Financial.Balance").Should().Be(FieldAccess.Allowed);
        _sut.CheckAccess(RoleType.Financial, "Financial.Payment").Should().Be(FieldAccess.Allowed);
    }

    [Fact]
    public void CheckAccess_UnspecifiedField_DefaultsToDenied()
    {
        _sut.CheckAccess(RoleType.Receptionist, "Student.UnknownField").Should().Be(FieldAccess.Denied);
        _sut.CheckAccess(RoleType.Instructor, "Student.UnknownField").Should().Be(FieldAccess.Denied);
        _sut.CheckAccess(RoleType.Financial, "Student.Name").Should().Be(FieldAccess.Denied); // Financial doesn't have Student.Name explicitly allowed
    }
}
