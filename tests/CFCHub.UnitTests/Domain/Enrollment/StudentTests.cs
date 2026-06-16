using System;
using System.Linq;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Enrollment.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Domain.Enrollment;

public class StudentTests
{
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;

    public StudentTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        
        _idGenerator = Substitute.For<IIdGenerator>();
        _idGenerator.NewId<StudentId>().Returns(new StudentId(Guid.NewGuid()));
    }

    private Student CreateValidStudent()
    {
        return Student.Create(
            new StudentId(Guid.NewGuid()),
            "John Doe",
            "12345678901",
            "123456789",
            "john@example.com",
            "11999999999",
            new DateOnly(1990, 1, 1),
            new Address("Street", "123", null, "District", "City", "ST", "12345678"),
            _clock,
            _idGenerator);
    }

    [Fact]
    public void Create_WithValidData_CreatesStudentAndRaisesEvent()
    {
        // Act
        var student = CreateValidStudent();

        // Assert
        student.Should().NotBeNull();
        student.Cpf.Should().Be("12345678901");
        student.Status.Should().Be(StudentStatus.Active);
        
        var events = student.DomainEvents.ToList();
        events.Should().ContainSingle();
        events.First().Should().BeOfType<StudentCreatedEvent>();
    }

    [Theory]
    [InlineData("1234567890")] // 10 digits
    [InlineData("123456789012")] // 12 digits
    [InlineData("1234567890a")] // alphanumeric
    [InlineData("123.456.789-01")] // formatted
    [InlineData("")]
    public void Create_WithInvalidCpf_ThrowsValidation(string invalidCpf)
    {
        // Act
        Action act = () => Student.Create(
            new StudentId(Guid.NewGuid()),
            "John Doe",
            invalidCpf,
            null,
            "john@example.com",
            "11999999999",
            new DateOnly(1990, 1, 1),
            new Address("Street", "123", null, "District", "City", "ST", "12345678"),
            _clock,
            _idGenerator);

        // Assert
        act.Should().Throw<ValidationException>().WithMessage("CPF must contain exactly 11 digits.");
    }

    [Fact]
    public void Anonymize_WhenNotPendingErasure_ThrowsUnprocessable()
    {
        // Arrange
        var student = CreateValidStudent();

        // Act
        Action act = () => student.Anonymize(_clock);

        // Assert
        act.Should().Throw<UnprocessableException>().WithMessage("Student can only be anonymized if status is PendingErasure.");
    }

    [Fact]
    public void Anonymize_SetsCpfToHash()
    {
        // Arrange
        var student = CreateValidStudent();
        student.RequestErasure();
        var originalCpf = student.Cpf;

        // Act
        student.Anonymize(_clock);

        // Assert
        student.Name.Should().Be("[REMOVIDO]");
        student.Email.Should().Be("[REMOVIDO]");
        student.Phone.Should().Be("[REMOVIDO]");
        student.Rg.Should().BeNull();
        student.HomeAddress.Should().Be(Address.Empty);
        
        student.Cpf.Should().NotBe(originalCpf);
        student.Cpf.Length.Should().Be(64); // SHA-256 hex string length
        
        var events = student.DomainEvents.ToList();
        events.Should().ContainSingle(e => e is StudentAnonymizedEvent);
    }
}
