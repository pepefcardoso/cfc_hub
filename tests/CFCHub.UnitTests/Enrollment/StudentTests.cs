using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Enrollment.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Enrollment;

public class StudentTests
{
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;

    public StudentTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
        _idGenerator = Substitute.For<IIdGenerator>();
    }

    [Fact]
    public void Student_Create_WithValidData_ReturnsStudent()
    {
        var address = new Address("Street", "123", "Complement", "District", "City", "State", "12345678");
        var id = new StudentId(Guid.NewGuid());
        var student = Student.Create(id, "Test", "12345678909", null, "test@example.com", "11999999999", new DateOnly(1990, 1, 1), address, _clock, _idGenerator);

        student.Should().NotBeNull();
        student.Cpf.Should().Be("12345678909");
        student.Status.Should().Be(StudentStatus.Active);
        student.DomainEvents.Should().ContainSingle(e => e is StudentCreatedEvent);
    }

    [Fact]
    public void Student_Create_WithInvalidCpfAlgorithm_ThrowsValidationException()
    {
        var address = new Address("Street", "123", "Complement", "District", "City", "State", "12345678");
        var id = new StudentId(Guid.NewGuid());

        Action act = () => Student.Create(id, "Test", "123A", null, "test@example.com", "11999999999", new DateOnly(1990, 1, 1), address, _clock, _idGenerator);

        act.Should().Throw<ValidationException>().WithMessage("*exactly 11 digits*");
    }

    [Fact]
    public void Student_Anonymize_WhenNotPendingErasure_ThrowsUnprocessable()
    {
        var student = new StudentBuilder().WithStatus(StudentStatus.Active).Build();

        Action act = () => student.Anonymize(_clock);

        act.Should().Throw<UnprocessableException>().WithMessage("*only be anonymized if status is PendingErasure*");
    }

    [Fact]
    public void Student_Anonymize_SetsCpfToSha256Hash()
    {
        var originalCpf = "12345678909";
        var student = new StudentBuilder().WithCpf(originalCpf).WithStatus(StudentStatus.PendingErasure).Build();

        student.Anonymize(_clock);

        student.Name.Should().Be("[REMOVIDO]");
        student.Email.Should().Be("[REMOVIDO]");
        student.Phone.Should().Be("[REMOVIDO]");
        student.Rg.Should().BeNull();
        student.HomeAddress.Should().Be(Address.Empty);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(originalCpf));
        var expectedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        student.Cpf.Should().Be(expectedHash);
        student.DomainEvents.Should().ContainSingle(e => e is StudentAnonymizedEvent);
    }

    [Fact]
    public void Student_SoftDelete_SetsDeletedAt()
    {
        var student = new StudentBuilder().Build();
        student.SoftDelete(_clock);
        student.DeletedAt.Should().Be(_clock.UtcNow);
    }

    [Fact]
    public void Student_RequestErasure_SetsStatusToPendingErasure()
    {
        var student = new StudentBuilder().WithStatus(StudentStatus.Active).Build();
        student.RequestErasure();
        student.Status.Should().Be(StudentStatus.PendingErasure);
    }
}
