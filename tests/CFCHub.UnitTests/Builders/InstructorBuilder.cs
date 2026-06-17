using System;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Scheduling;

namespace CFCHub.UnitTests.Builders;

public class InstructorBuilder
{
    private InstructorId _id = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private StaffUserId _linkedUserId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    private string _name = "John Doe";
    private CnhCategory[] _categories = { CnhCategory.B };
    private int _maxDailySlots = 8;
    private InstructorAvailabilityTemplate? _template;
    private readonly System.Collections.Generic.List<DayAvailabilityOverride> _overrides = new();

    public InstructorBuilder WithTemplate(InstructorAvailabilityTemplate template)
    {
        _template = template;
        return this;
    }

    public InstructorBuilder WithDayOverride(DayAvailabilityOverride dayOverride)
    {
        _overrides.Add(dayOverride);
        return this;
    }

    public Instructor Build()
    {
        var instructor = new Instructor(_id, _linkedUserId, _name, _categories, _maxDailySlots);
        if (_template != null)
        {
            instructor.SetAvailabilityTemplate(_template);
        }
        foreach (var overrideItem in _overrides)
        {
            instructor.AddDayOverride(overrideItem);
        }
        return instructor;
    }
}
