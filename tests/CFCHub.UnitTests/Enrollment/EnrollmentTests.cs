using System;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Enrollment.Events;
using CFCHub.Domain.Shared;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Enrollment;

public class EnrollmentTests
{
    private readonly ISystemClock _clock;

    public EnrollmentTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Enrollment_Enroll_ReturnsEnrollment()
    {
        var id = new EnrollmentId(Guid.NewGuid());
        var studentId = new StudentId(Guid.NewGuid());
        
        var enrollment = CFCHub.Domain.Enrollment.Enrollment.Enroll(id, studentId, CFCHub.Domain.Scheduling.CnhCategory.B, _clock);

        enrollment.Should().NotBeNull();
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.DomainEvents.Should().ContainSingle(e => e is StudentEnrolledEvent);
    }

    [Fact]
    public void Enrollment_IncrementPracticalHours_WhenBelowThreshold_DoesNotComplete()
    {
        var enrollment = new EnrollmentBuilder()
            .WithCategory(CFCHub.Domain.Scheduling.CnhCategory.B)
            .WithPracticalHoursCompleted(18)
            .WithStatus(EnrollmentStatus.Active)
            .Build();

        enrollment.IncrementPracticalHours(_clock);

        enrollment.PracticalHoursCompleted.Should().Be(19);
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Enrollment_IncrementPracticalHours_WhenReachesThreshold_Completes()
    {
        var enrollment = new EnrollmentBuilder()
            .WithCategory(CFCHub.Domain.Scheduling.CnhCategory.B)
            .WithPracticalHoursCompleted(19)
            .WithStatus(EnrollmentStatus.Active)
            .Build();

        enrollment.IncrementPracticalHours(_clock);

        enrollment.PracticalHoursCompleted.Should().Be(20);
        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        enrollment.CompletedAt.Should().Be(_clock.UtcNow);
        enrollment.DomainEvents.Should().ContainSingle(e => e is EnrollmentCompletedEvent);
    }

    [Fact]
    public void Enrollment_Complete_WhenAlreadyCompleted_DoesNothing()
    {
        var enrollment = new EnrollmentBuilder()
            .WithStatus(EnrollmentStatus.Completed)
            .Build();

        enrollment.Complete(_clock);

        enrollment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Enrollment_SoftDelete_SetsDeletedAt()
    {
        var enrollment = new EnrollmentBuilder().Build();
        enrollment.SoftDelete(_clock);
        enrollment.DeletedAt.Should().Be(_clock.UtcNow);
    }
}
