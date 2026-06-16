using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment.Events;

public record StudentCreatedEvent(StudentId StudentId, DateTimeOffset OccurredAt) : IDomainEvent;
