using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Scheduling;
using CFCHub.Domain.Enrollment.Events;

namespace CFCHub.Domain.Enrollment;

public class Enrollment : AggregateRoot<EnrollmentId>, ISoftDeletable, IAuditable
{
    public StudentId StudentId { get; private set; }
    public CnhCategory Category { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public int TheoryHoursCompleted { get; private set; }
    public int PracticalHoursCompleted { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

#pragma warning disable CS8618 // EF Core
    private Enrollment() { }
#pragma warning restore CS8618

    private Enrollment(EnrollmentId id, StudentId studentId, CnhCategory category) : base(id)
    {
        StudentId = studentId;
        Category = category;
        Status = EnrollmentStatus.Active;
        TheoryHoursCompleted = 0;
        PracticalHoursCompleted = 0;
    }

    public static Enrollment Enroll(EnrollmentId id, StudentId studentId, CnhCategory category, ISystemClock clock)
    {
        var enrollment = new Enrollment(id, studentId, category);
        enrollment.AddDomainEvent(new StudentEnrolledEvent(id, studentId, category, clock.UtcNow));
        return enrollment;
    }

    public void IncrementPracticalHours(ISystemClock clock)
    {
        PracticalHoursCompleted++;
        
        if (PracticalHoursCompleted >= GetPracticalHoursThreshold())
        {
            Complete(clock);
        }
    }

    public void Complete(ISystemClock clock)
    {
        if (Status == EnrollmentStatus.Completed)
        {
            return;
        }

        Status = EnrollmentStatus.Completed;
        CompletedAt = clock.UtcNow;
        AddDomainEvent(new EnrollmentCompletedEvent(Id, StudentId, clock.UtcNow));
    }

    public void SoftDelete(ISystemClock clock)
    {
        DeletedAt = clock.UtcNow;
    }

    private int GetPracticalHoursThreshold()
    {
        return Category switch
        {
            CnhCategory.A => 20,
            CnhCategory.B => 20,
            CnhCategory.C => 20,
            CnhCategory.D => 20,
            CnhCategory.E => 20,
            CnhCategory.ACC => 20,
            _ => 20
        };
    }
}
