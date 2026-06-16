using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Identity.Events;

public record WelcomeEmailRequestedEvent(
    StaffUserId StaffUserId,
    string PlainTextPassword,
    DateTimeOffset OccurredAt) : IDomainEvent;
