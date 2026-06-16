using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment.Events;

public record StudentWelcomeEmailRequestedEvent(StudentId StudentId, DateTimeOffset OccurredAt) : IDomainEvent;
