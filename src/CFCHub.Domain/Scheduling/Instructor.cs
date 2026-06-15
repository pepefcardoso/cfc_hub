using System;
using System.Collections.Generic;
using System.Linq;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public class Instructor : AggregateRoot<InstructorId>
{
    public StaffUserId LinkedUserId { get; private set; }
    public string Name { get; private set; }
    
    private readonly List<CnhCategory> _teachableCategories = new();
    public IReadOnlyList<CnhCategory> TeachableCategories => _teachableCategories.AsReadOnly();
    
    public int MaxDailySlots { get; private set; }
    public InstructorAvailabilityTemplate WeeklyTemplate { get; private set; }

    private readonly List<DayAvailabilityOverride> _overrides = new();
    public IReadOnlyList<DayAvailabilityOverride> Overrides => _overrides.AsReadOnly();

    private Instructor() 
    { 
        LinkedUserId = null!;
        Name = null!;
        WeeklyTemplate = null!;
    }

    public Instructor(InstructorId id, StaffUserId linkedUserId, string name, IEnumerable<CnhCategory> teachableCategories, int maxDailySlots) : base(id)
    {
        LinkedUserId = linkedUserId;
        Name = name;
        _teachableCategories.AddRange(teachableCategories);
        MaxDailySlots = maxDailySlots;
        WeeklyTemplate = new InstructorAvailabilityTemplate(Array.Empty<AvailabilityWindow>());
    }

    public void SetAvailabilityTemplate(InstructorAvailabilityTemplate template)
    {
        WeeklyTemplate = template;
    }

    public void AddDayOverride(DayAvailabilityOverride dayOverride)
    {
        var existing = _overrides.FirstOrDefault(o => o.Date == dayOverride.Date);
        if (existing != null)
        {
            _overrides.Remove(existing);
        }
        _overrides.Add(dayOverride);
    }

    public bool IsAvailableAt(DateTimeOffset time, ISystemClock clock)
    {
        if (time < clock.UtcNow) return false;

        var date = DateOnly.FromDateTime(time.Date);
        var timeOfDay = TimeOnly.FromTimeSpan(time.TimeOfDay);
        var dayOfWeek = time.DayOfWeek;

        var dayOverride = _overrides.FirstOrDefault(o => o.Date == date);
        if (dayOverride != null)
        {
            if (!dayOverride.IsAvailable) return false;

            if (dayOverride.CustomWindows.Any())
            {
                return dayOverride.CustomWindows.Any(w => timeOfDay >= w.Start && timeOfDay < w.End);
            }
        }

        return WeeklyTemplate.Windows.Any(w => w.DayOfWeek == dayOfWeek && timeOfDay >= w.Start && timeOfDay < w.End);
    }
}
