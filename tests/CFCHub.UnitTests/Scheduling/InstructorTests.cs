using System;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Scheduling;

public class InstructorTests
{
    private readonly ISystemClock _clock;

    public InstructorTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void IsAvailableAt_WithDayOverride_ReturnsFalse()
    {
        // Arrange
        var template = new InstructorAvailabilityTemplate(new[]
        {
            new AvailabilityWindow(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(18, 0))
        });

        var instructor = new Builders.InstructorBuilder()
            .WithTemplate(template)
            .Build();

        // Monday, June 15th 2026 at 10:00
        var time = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(-3));
        
        // Add override for that day to be unavailable
        instructor.AddDayOverride(new DayAvailabilityOverride(DateOnly.FromDateTime(time.Date), false));

        // Act
        var result = instructor.IsAvailableAt(time, _clock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAvailableAt_OutsideWeeklyTemplate_ReturnsFalse()
    {
        // Arrange
        var template = new InstructorAvailabilityTemplate(new[]
        {
            new AvailabilityWindow(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0))
        });

        var instructor = new Builders.InstructorBuilder()
            .WithTemplate(template)
            .Build();

        // Monday, June 15th 2026 at 14:00 (outside window)
        var time = new DateTimeOffset(2026, 6, 15, 14, 0, 0, TimeSpan.FromHours(-3));

        // Act
        var result = instructor.IsAvailableAt(time, _clock);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsAvailableAt_InsideWeeklyTemplate_ReturnsTrue()
    {
        // Arrange
        var template = new InstructorAvailabilityTemplate(new[]
        {
            new AvailabilityWindow(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(18, 0))
        });

        var instructor = new Builders.InstructorBuilder()
            .WithTemplate(template)
            .Build();

        // Monday, June 15th 2026 at 10:00
        var time = new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(-3));

        // Act
        var result = instructor.IsAvailableAt(time, _clock);

        // Assert
        result.Should().BeTrue();
    }
}
