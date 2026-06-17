using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Students;
using CFCHub.Domain.Scheduling.Events;

namespace CFCHub.Domain.Scheduling;

public sealed class SchedulingSlot : AggregateRoot<SchedulingSlotId>
{
    public InstructorId InstructorId { get; private set; }
    public VehicleId VehicleId { get; private set; }
    public TrackId TrackId { get; private set; }
    public StudentId StudentId { get; private set; }
    public CnhCategory Category { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset EndedAt { get; private set; }
    public SlotStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? ReminderSentAt { get; private set; }

    private SchedulingSlot(
        SchedulingSlotId id,
        InstructorId instructorId,
        VehicleId vehicleId,
        TrackId trackId,
        StudentId studentId,
        DateTimeOffset startedAt,
        CnhCategory category) : base(id)
    {
        InstructorId = instructorId;
        VehicleId = vehicleId;
        TrackId = trackId;
        StudentId = studentId;
        StartedAt = startedAt;
        EndedAt = startedAt.AddMinutes(50);
        Category = category;
        Status = SlotStatus.Confirmed;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private SchedulingSlot() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public static SchedulingSlot Book(
        SchedulingSlotId id,
        InstructorId instructorId,
        VehicleId vehicleId,
        TrackId trackId,
        StudentId studentId,
        DateTimeOffset startedAt,
        CnhCategory category,
        ISystemClock clock)
    {
        if (startedAt < clock.UtcNow)
        {
            throw new UnprocessableException("Slot cannot be booked in the past.", "SLOT_IN_PAST");
        }

        if (startedAt.Minute != 0 && startedAt.Minute != 50)
        {
            throw new UnprocessableException("Slot time must be on a 50-minute boundary (e.g., :00 or :50).", "INVALID_SLOT_TIME");
        }

        var slot = new SchedulingSlot(id, instructorId, vehicleId, trackId, studentId, startedAt, category);

        slot.AddDomainEvent(new SchedulingSlotBookedEvent(
            slot.Id,
            slot.InstructorId,
            slot.VehicleId,
            slot.TrackId,
            slot.StudentId,
            slot.StartedAt,
            slot.EndedAt,
            clock.UtcNow));

        return slot;
    }

    public void Cancel(string reason, ISystemClock clock)
    {
        if (Status == SlotStatus.Completed)
        {
            throw new UnprocessableException("Cannot cancel a completed slot.", "SLOT_ALREADY_COMPLETED");
        }
        
        if (Status == SlotStatus.Cancelled)
        {
            throw new UnprocessableException("Cannot cancel an already cancelled slot.", "SLOT_ALREADY_CANCELLED");
        }

        Status = SlotStatus.Cancelled;
        CancellationReason = reason;

        AddDomainEvent(new SchedulingSlotCancelledEvent(Id, reason, clock.UtcNow));
    }

    public void Complete(ISystemClock clock)
    {
        if (Status != SlotStatus.Confirmed)
        {
            throw new UnprocessableException("Only confirmed slots can be completed.", "SLOT_NOT_CONFIRMED");
        }

        Status = SlotStatus.Completed;

        AddDomainEvent(new SchedulingSlotCompletedEvent(Id, clock.UtcNow));
    }

    public void MarkNoShow()
    {
        if (Status != SlotStatus.Confirmed)
        {
            throw new UnprocessableException("Only confirmed slots can be marked as no-show.", "SLOT_NOT_CONFIRMED");
        }

        Status = SlotStatus.NoShow;
    }

    public void MarkReminderSent(ISystemClock clock)
    {
        if (ReminderSentAt.HasValue)
        {
            throw new UnprocessableException("Reminder already sent for this slot.", "REMINDER_ALREADY_SENT");
        }

        ReminderSentAt = clock.UtcNow;

        AddDomainEvent(new SchedulingSlotReminderRequestedEvent(
            Id,
            StudentId.Value,
            StartedAt,
            clock.UtcNow));
    }
}
