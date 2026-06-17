using System;
using CFCHub.Domain.Contracts;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Finance;
using CFCHub.Domain.Shared;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests;

public class DomainCoverageTests
{
    private readonly ISystemClock _clock;

    public DomainCoverageTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Address_EfCoreConstructor_Exists()
    {
        var instance = Activator.CreateInstance(typeof(Address), true);
        instance.Should().NotBeNull();
    }

    [Fact]
    public void ConsentRecord_EfCoreConstructor_Exists()
    {
        var instance = Activator.CreateInstance(typeof(ConsentRecord), true);
        instance.Should().NotBeNull();
    }

    [Fact]
    public void SignatureRecord_EfCoreConstructor_Exists()
    {
        var instance = Activator.CreateInstance(typeof(SignatureRecord), true);
        instance.Should().NotBeNull();
    }

    [Fact]
    public void Money_EfCoreConstructor_Exists()
    {
        var instance = Activator.CreateInstance(typeof(Money), true);
        instance.Should().NotBeNull();
    }

    [Fact]
    public void Student_PropertiesAndEfConstructor()
    {
        var student = new StudentBuilder().Build();
        student.CreatedAt = _clock.UtcNow;
        student.CreatedBy = "sys";
        student.UpdatedAt = _clock.UtcNow;
        student.UpdatedBy = "sys2";

        student.CreatedAt.Should().Be(_clock.UtcNow);
        student.CreatedBy.Should().Be("sys");
        student.UpdatedAt.Should().Be(_clock.UtcNow);
        student.UpdatedBy.Should().Be("sys2");

        var instance = Activator.CreateInstance(typeof(Student), true);
        instance.Should().NotBeNull();
    }

    [Fact]
    public void Contract_PropertiesAndEfConstructor()
    {
        var contract = new ContractBuilder().Build();
        contract.CreatedAt = _clock.UtcNow;
        contract.UpdatedAt = _clock.UtcNow;

        contract.CreatedAt.Should().Be(_clock.UtcNow);
        contract.UpdatedAt.Should().Be(_clock.UtcNow);

        var instance = Activator.CreateInstance(typeof(Contract), true);
        instance.Should().NotBeNull();
    }

    [Fact]
    public void Payment_PropertiesAndEfConstructor()
    {
        var payment = new PaymentBuilder().Build();
        payment.CreatedAt = _clock.UtcNow;
        payment.CreatedBy = "user";
        payment.UpdatedAt = _clock.UtcNow;
        payment.UpdatedBy = "user2";

        payment.CreatedAt.Should().Be(_clock.UtcNow);
        payment.CreatedBy.Should().Be("user");
        payment.UpdatedAt.Should().Be(_clock.UtcNow);
        payment.UpdatedBy.Should().Be("user2");

        var instance = Activator.CreateInstance(typeof(Payment), true);
        instance.Should().NotBeNull();
    }

    [Theory]
    [InlineData(CFCHub.Domain.Scheduling.CnhCategory.A, 20)]
    [InlineData(CFCHub.Domain.Scheduling.CnhCategory.B, 20)]
    [InlineData(CFCHub.Domain.Scheduling.CnhCategory.C, 20)]
    [InlineData(CFCHub.Domain.Scheduling.CnhCategory.D, 20)]
    [InlineData(CFCHub.Domain.Scheduling.CnhCategory.E, 20)]
    [InlineData(CFCHub.Domain.Scheduling.CnhCategory.ACC, 20)]
    [InlineData((CFCHub.Domain.Scheduling.CnhCategory)999, 20)]
    public void Enrollment_IncrementPracticalHours_Thresholds(CFCHub.Domain.Scheduling.CnhCategory category, int expectedThreshold)
    {
        var enrollment = new EnrollmentBuilder()
            .WithCategory(category)
            .WithPracticalHoursCompleted(expectedThreshold - 1)
            .WithStatus(EnrollmentStatus.Active)
            .Build();

        enrollment.IncrementPracticalHours(_clock);
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
    }

    [Fact]
    public void Enrollment_PropertiesAndEfConstructor()
    {
        var enrollment = new EnrollmentBuilder().Build();
        enrollment.CreatedAt = _clock.UtcNow;
        enrollment.UpdatedAt = _clock.UtcNow;

        enrollment.CreatedAt.Should().Be(_clock.UtcNow);
        enrollment.UpdatedAt.Should().Be(_clock.UtcNow);

        var instance = Activator.CreateInstance(typeof(CFCHub.Domain.Enrollment.Enrollment), true);
        instance.Should().NotBeNull();
    }
}
