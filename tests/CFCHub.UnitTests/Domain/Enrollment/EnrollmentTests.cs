using System;
using System.Linq;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Enrollment.Events;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Domain.Enrollment;

public class EnrollmentTests
{
    private readonly ISystemClock _clock;

    public EnrollmentTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Enroll_RaisesStudentEnrolledEvent()
    {
        // Arrange
        var enrollmentId = EnrollmentId.New();
        var studentId = new StudentId(Guid.NewGuid());
        var category = CnhCategory.B;

        // Act
        var enrollment = CFCHub.Domain.Enrollment.Enrollment.Enroll(enrollmentId, studentId, category, _clock);

        // Assert
        enrollment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StudentEnrolledEvent>();
            
        var @event = (StudentEnrolledEvent)enrollment.DomainEvents.First();
        @event.EnrollmentId.Should().Be(enrollmentId);
        @event.StudentId.Should().Be(studentId);
        @event.Category.Should().Be(category);
    }

    [Fact]
    public void IncrementPracticalHours_WhenBelowThreshold_DoesNotComplete()
    {
        // Arrange
        var enrollmentId = EnrollmentId.New();
        var studentId = new StudentId(Guid.NewGuid());
        var category = CnhCategory.B; // threshold is 20
        var enrollment = CFCHub.Domain.Enrollment.Enrollment.Enroll(enrollmentId, studentId, category, _clock);
        enrollment.ClearDomainEvents();

        // Act
        enrollment.IncrementPracticalHours(_clock);

        // Assert
        enrollment.PracticalHoursCompleted.Should().Be(1);
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.DomainEvents.Should().NotContain(e => e is EnrollmentCompletedEvent);
    }
}
