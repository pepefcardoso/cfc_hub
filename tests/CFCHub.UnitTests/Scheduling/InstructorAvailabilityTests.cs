using System;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Scheduling;

public class InstructorAvailabilityTests
{
    private readonly ISystemClock _clock;

    public InstructorAvailabilityTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void InstructorAvailability_WithDayOverride_OverridesWeeklyTemplate()
    {
        // Arrange
        // Let's set up an instructor available on Mondays 08:00 - 12:00
        var template = new InstructorAvailabilityTemplate(new[]
        {
            new AvailabilityWindow(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))
        });

        // Let's create an override for a specific Monday, making them UNAVAILABLE
        // The clock is at 2026-06-17 (Wednesday). Next Monday is 2026-06-22.
        var nextMonday = new DateOnly(2026, 6, 22);
        var dayOverride = new DayAvailabilityOverride(nextMonday, isAvailable: false);

        var instructor = new InstructorBuilder()
            .WithTemplate(template)
            .WithDayOverride(dayOverride)
            .Build();

        // Target time is Monday at 10:00 (which is inside the template window)
        var targetTime = new DateTimeOffset(2026, 6, 22, 10, 0, 0, TimeSpan.Zero);

        // Act
        var isAvailable = instructor.IsAvailableAt(targetTime, _clock);

        // Assert
        isAvailable.Should().BeFalse();
    }
}
