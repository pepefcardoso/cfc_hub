using System;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Students;

namespace CFCHub.UnitTests.Builders;

public class SchedulingSlotBuilder
{
    private SchedulingSlotId _id = new(Guid.NewGuid());
    private InstructorId _instructorId = new(Guid.NewGuid());
    private VehicleId _vehicleId = new(Guid.NewGuid());
    private TrackId _trackId = new(Guid.NewGuid());
    private StudentId _studentId = new(Guid.NewGuid());
    private DateTimeOffset _startedAt;
    private CnhCategory _category = CnhCategory.B;
    private ISystemClock _clock;

    public SchedulingSlotBuilder(ISystemClock clock)
    {
        _clock = clock;
        _startedAt = clock.UtcNow.AddDays(1).Date.AddHours(10); // Next day at 10:00
    }

    public SchedulingSlotBuilder WithStartedAt(DateTimeOffset startedAt)
    {
        _startedAt = startedAt;
        return this;
    }

    public SchedulingSlot Build()
    {
        return SchedulingSlot.Book(
            _id, 
            _instructorId, 
            _vehicleId, 
            _trackId, 
            _studentId, 
            _startedAt, 
            _category, 
            _clock);
    }
}
