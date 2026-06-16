using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Scheduling;

namespace CFCHub.Domain.Enrollment.Events;

public record StudentEnrolledEvent(
    EnrollmentId EnrollmentId,
    StudentId StudentId,
    CnhCategory Category,
    DateTimeOffset OccurredOn) : IDomainEvent
{
    public DateTimeOffset OccurredAt => OccurredOn;
}
