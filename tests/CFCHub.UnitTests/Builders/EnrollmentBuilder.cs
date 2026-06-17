using System;
using System.Reflection;
using CFCHub.Domain.Enrollment;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Scheduling;
using EnrollmentEntity = CFCHub.Domain.Enrollment.Enrollment;

namespace CFCHub.UnitTests.Builders;

public class EnrollmentBuilder
{
    private EnrollmentId _id = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private StudentId _studentId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private CnhCategory _category = CnhCategory.B;
    private EnrollmentStatus _status = EnrollmentStatus.Active;
    private int _practicalHoursCompleted = 0;

    public EnrollmentBuilder WithId(EnrollmentId id)
    {
        _id = id;
        return this;
    }

    public EnrollmentBuilder WithStudentId(StudentId studentId)
    {
        _studentId = studentId;
        return this;
    }

    public EnrollmentBuilder WithCategory(CnhCategory category)
    {
        _category = category;
        return this;
    }

    public EnrollmentBuilder WithStatus(EnrollmentStatus status)
    {
        _status = status;
        return this;
    }

    public EnrollmentBuilder WithPracticalHoursCompleted(int hours)
    {
        _practicalHoursCompleted = hours;
        return this;
    }

    public EnrollmentEntity Build()
    {
        var ctor = typeof(EnrollmentEntity).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] 
        { 
            typeof(EnrollmentId), typeof(StudentId), typeof(CnhCategory)
        }, null);
        
        if (ctor == null)
            throw new InvalidOperationException("Enrollment constructor not found.");

        var enrollment = (EnrollmentEntity)ctor.Invoke(new object?[] { _id, _studentId, _category });
        
        typeof(EnrollmentEntity).GetProperty(nameof(EnrollmentEntity.Status))!.SetValue(enrollment, _status);
        typeof(EnrollmentEntity).GetProperty(nameof(EnrollmentEntity.PracticalHoursCompleted))!.SetValue(enrollment, _practicalHoursCompleted);
        
        return enrollment;
    }
}
