using System;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Scheduling.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Students;
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
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 6, 16, 10, 0, 0, TimeSpan.Zero));
    }

    private static SchedulingSlotId CreateId() => new(Guid.NewGuid());
    private static InstructorId CreateInstructorId() => new(Guid.NewGuid());
    private static VehicleId CreateVehicleId() => new(Guid.NewGuid());
    private static TrackId CreateTrackId() => new(Guid.NewGuid());
    private static StudentId CreateStudentId() => new(Guid.NewGuid());

    [Fact]
    public void Book_WithPastTime_ThrowsUnprocessable()
    {
        var pastTime = _clock.UtcNow.AddMinutes(-50);

        Action act = () => SchedulingSlot.Book(
            CreateId(),
            CreateInstructorId(),
            CreateVehicleId(),
            CreateTrackId(),
            CreateStudentId(),
            pastTime,
            CnhCategory.B,
            _clock);

        act.Should().Throw<UnprocessableException>()
            .Where(e => e.ErrorCode == "SLOT_IN_PAST");
    }

    [Fact]
    public void Book_WithInvalidMinute_ThrowsUnprocessable()
    {
        var invalidTime = _clock.UtcNow.AddHours(1).AddMinutes(15);

        Action act = () => SchedulingSlot.Book(
            CreateId(),
            CreateInstructorId(),
            CreateVehicleId(),
            CreateTrackId(),
            CreateStudentId(),
            invalidTime,
            CnhCategory.B,
            _clock);

        act.Should().Throw<UnprocessableException>()
            .Where(e => e.ErrorCode == "INVALID_SLOT_TIME");
    }

    [Fact]
    public void Book_RaisesSlotBookedEvent()
    {
        var startTime = _clock.UtcNow.AddHours(1); // 11:00

        var slot = SchedulingSlot.Book(
            CreateId(),
            CreateInstructorId(),
            CreateVehicleId(),
            CreateTrackId(),
            CreateStudentId(),
            startTime,
            CnhCategory.B,
            _clock);

        slot.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SchedulingSlotBookedEvent>();
    }

    [Fact]
    public void Book_SetsEndedAtCorrectly()
    {
        var startTime = _clock.UtcNow.AddHours(1); // 11:00

        var slot = SchedulingSlot.Book(
            CreateId(),
            CreateInstructorId(),
            CreateVehicleId(),
            CreateTrackId(),
            CreateStudentId(),
            startTime,
            CnhCategory.B,
            _clock);

        slot.EndedAt.Should().Be(startTime.AddMinutes(50));
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsUnprocessable()
    {
        var slot = SchedulingSlot.Book(
            CreateId(),
            CreateInstructorId(),
            CreateVehicleId(),
            CreateTrackId(),
            CreateStudentId(),
            _clock.UtcNow.AddHours(1),
            CnhCategory.B,
            _clock);

        slot.Complete(_clock);

        Action act = () => slot.Cancel("Reason", _clock);

        act.Should().Throw<UnprocessableException>()
            .Where(e => e.ErrorCode == "SLOT_ALREADY_COMPLETED");
    }

    [Fact]
    public void Cancel_SetsStatusCancelled_AndRaisesEvent()
    {
        var slot = SchedulingSlot.Book(
            CreateId(),
            CreateInstructorId(),
            CreateVehicleId(),
            CreateTrackId(),
            CreateStudentId(),
            _clock.UtcNow.AddHours(1),
            CnhCategory.B,
            _clock);

        slot.ClearDomainEvents();

        slot.Cancel("Reason", _clock);

        slot.Status.Should().Be(SlotStatus.Cancelled);
        slot.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SchedulingSlotCancelledEvent>();
    }

    [Fact]
    public void Complete_WhenConfirmed_SetsStatusCompleted()
    {
        var slot = SchedulingSlot.Book(
            CreateId(),
            CreateInstructorId(),
            CreateVehicleId(),
            CreateTrackId(),
            CreateStudentId(),
            _clock.UtcNow.AddHours(1),
            CnhCategory.B,
            _clock);

        slot.ClearDomainEvents();

        slot.Complete(_clock);

        slot.Status.Should().Be(SlotStatus.Completed);
        slot.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SchedulingSlotCompletedEvent>();
    }

    [Fact]
    public void MarkNoShow_WhenConfirmed_SetsStatusNoShow()
    {
        var slot = SchedulingSlot.Book(
            CreateId(),
            CreateInstructorId(),
            CreateVehicleId(),
            CreateTrackId(),
            CreateStudentId(),
            _clock.UtcNow.AddHours(1),
            CnhCategory.B,
            _clock);

        slot.ClearDomainEvents();

        slot.MarkNoShow();

        slot.Status.Should().Be(SlotStatus.NoShow);
        slot.DomainEvents.Should().BeEmpty();
    }
}
