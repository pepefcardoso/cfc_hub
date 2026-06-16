using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Enrollment.Events;

public record StudentAnonymizedEvent(StudentId StudentId, DateTimeOffset OccurredAt) : IDomainEvent;
