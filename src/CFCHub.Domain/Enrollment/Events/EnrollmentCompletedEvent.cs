using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment.Events;

public record EnrollmentCompletedEvent(
    EnrollmentId EnrollmentId,
    StudentId StudentId,
    DateTimeOffset CompletedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt => CompletedAt;
}
