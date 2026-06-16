using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Scheduling.Specifications;
using CFCHub.Domain.Students;
using NSubstitute;
using FluentAssertions;
using Xunit;

namespace CFCHub.UnitTests.Domain.Scheduling.Specifications;

public class SlotOverlapSpecTests
{
    private readonly ISystemClock _clock;
    private readonly SlotOverlapSpec _spec;

    public SlotOverlapSpecTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _spec = new SlotOverlapSpec();
    }

    [Fact]
    public void SlotOverlapSpec_WithAdjacentSlots_ReturnsFalse()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(now);
        
        var slotStart = now.AddHours(1); // 11:00
        var existing = SchedulingSlot.Book(
            new SchedulingSlotId(Guid.NewGuid()),
            new InstructorId(Guid.NewGuid()),
            new VehicleId(Guid.NewGuid()),
            new TrackId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            slotStart,
            CnhCategory.B,
            _clock);
            
        // Existing slot is 11:00 to 11:50.
        // New slot starting at 11:50 should not overlap.
        var newStart1 = slotStart.AddMinutes(50); 
        // New slot starting at 10:10 (ends at 11:00) should not overlap.
        var newStart2 = slotStart.AddMinutes(-50); 

        // Act
        var result1 = _spec.IsSatisfiedBy(existing, newStart1);
        var result2 = _spec.IsSatisfiedBy(existing, newStart2);

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public void SlotOverlapSpec_WithOverlappingSlots_ReturnsTrue()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 6, 16, 10, 0, 0, TimeSpan.Zero);
        _clock.UtcNow.Returns(now);
        
        var slotStart = now.AddHours(1); // 11:00
        var existing = SchedulingSlot.Book(
            new SchedulingSlotId(Guid.NewGuid()),
            new InstructorId(Guid.NewGuid()),
            new VehicleId(Guid.NewGuid()),
            new TrackId(Guid.NewGuid()),
            new StudentId(Guid.NewGuid()),
            slotStart,
            CnhCategory.B,
            _clock);
            
        // Existing slot is 11:00 to 11:50.
        
        // 1. Exact same time
        var newStart1 = slotStart; 
        
        // 2. Starts during existing (11:10 -> 12:00)
        var newStart2 = slotStart.AddMinutes(10); 
        
        // 3. Ends during existing (10:50 -> 11:40)
        var newStart3 = slotStart.AddMinutes(-10);

        // Act
        var result1 = _spec.IsSatisfiedBy(existing, newStart1);
        var result2 = _spec.IsSatisfiedBy(existing, newStart2);
        var result3 = _spec.IsSatisfiedBy(existing, newStart3);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
    }
}
