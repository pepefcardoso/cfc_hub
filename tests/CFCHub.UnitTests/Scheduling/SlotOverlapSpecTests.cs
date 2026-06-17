using System;
using CFCHub.Domain.Scheduling.Specifications;
using CFCHub.Domain.Shared;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Scheduling;

public class SlotOverlapSpecTests
{
    private readonly ISystemClock _clock;

    public SlotOverlapSpecTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void SlotOverlapSpec_WithOverlappingSlots_ReturnsTrue()
    {
        // Arrange
        var startTime = _clock.UtcNow.AddDays(1).Date.AddHours(10); // 10:00
        var existingSlot = new SchedulingSlotBuilder(_clock).WithStartedAt(startTime).Build(); // Ends at 10:50
        
        var spec = new SlotOverlapSpec();
        var newStartTime = startTime.AddMinutes(20); // 10:20 (overlaps with 10:00-10:50)

        // Act
        var result = spec.IsSatisfiedBy(existingSlot, newStartTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SlotOverlapSpec_WithAdjacentSlots_ReturnsFalse()
    {
        // Arrange
        var startTime = _clock.UtcNow.AddDays(1).Date.AddHours(10); // 10:00
        var existingSlot = new SchedulingSlotBuilder(_clock).WithStartedAt(startTime).Build(); // Ends at 10:50
        
        var spec = new SlotOverlapSpec();
        var newStartTime = startTime.AddMinutes(50); // 10:50 (adjacent to 10:00-10:50)

        // Act
        var result = spec.IsSatisfiedBy(existingSlot, newStartTime);

        // Assert
        result.Should().BeFalse();
    }
}
