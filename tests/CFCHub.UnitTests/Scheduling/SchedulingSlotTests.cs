using System;
using System.Linq;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Scheduling.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Students;
using CFCHub.UnitTests.Builders;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Scheduling;

public class SchedulingSlotTests
{
    private readonly ISystemClock _clock;

    public SchedulingSlotTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 17, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Book_WithPastStartTime_ThrowsUnprocessableException()
    {
        var pastTime = _clock.UtcNow.AddMinutes(-50);
        var builder = new SchedulingSlotBuilder(_clock);

        Action act = () => builder.WithStartedAt(pastTime).Build();

        act.Should().Throw<UnprocessableException>()
            .Where(e => e.ErrorCode == "SLOT_IN_PAST");
    }

    [Fact]
    public void Book_WithValidData_SetsEndedAtTo50MinutesAfterStart()
    {
        var startTime = _clock.UtcNow.AddDays(1).Date.AddHours(10); // 10:00
        var builder = new SchedulingSlotBuilder(_clock);

        var slot = builder.WithStartedAt(startTime).Build();

        slot.EndedAt.Should().Be(startTime.AddMinutes(50));
    }

    [Fact]
    public void Book_WithValidData_RaisesSlotBookedEvent()
    {
        var startTime = _clock.UtcNow.AddDays(1).Date.AddHours(10);
        var builder = new SchedulingSlotBuilder(_clock);

        var slot = builder.WithStartedAt(startTime).Build();

        slot.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SchedulingSlotBookedEvent>();
    }

    [Fact]
    public void Cancel_WhenConfirmed_SetsStatusToCancelled()
    {
        var slot = new SchedulingSlotBuilder(_clock).Build();
        slot.ClearDomainEvents();

        slot.Cancel("Reason", _clock);

        slot.Status.Should().Be(SlotStatus.Cancelled);
        slot.CancellationReason.Should().Be("Reason");
        slot.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SchedulingSlotCancelledEvent>();
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsUnprocessableException()
    {
        var slot = new SchedulingSlotBuilder(_clock).Build();
        slot.Complete(_clock);

        Action act = () => slot.Cancel("Reason", _clock);

        act.Should().Throw<UnprocessableException>()
            .Where(e => e.ErrorCode == "SLOT_ALREADY_COMPLETED");
    }

    [Fact]
    public void Cancel_WhenCancelled_ThrowsUnprocessableException()
    {
        var slot = new SchedulingSlotBuilder(_clock).Build();
        slot.Cancel("Reason 1", _clock);

        Action act = () => slot.Cancel("Reason 2", _clock);

        act.Should().Throw<UnprocessableException>()
            .Where(e => e.ErrorCode == "SLOT_ALREADY_CANCELLED");
    }

    [Fact]
    public void Complete_WhenConfirmed_RaisesSlotCompletedEvent()
    {
        var slot = new SchedulingSlotBuilder(_clock).Build();
        slot.ClearDomainEvents();

        slot.Complete(_clock);

        slot.Status.Should().Be(SlotStatus.Completed);
        slot.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SchedulingSlotCompletedEvent>();
    }

    [Fact]
    public void MarkNoShow_WhenConfirmed_SetsStatusToNoShow()
    {
        var slot = new SchedulingSlotBuilder(_clock).Build();
        slot.ClearDomainEvents();

        slot.MarkNoShow();

        slot.Status.Should().Be(SlotStatus.NoShow);
        slot.DomainEvents.Should().BeEmpty();
    }
}
