using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Identity.Events;

public record StaffUserCreatedEvent(
    StaffUserId StaffUserId,
    RoleType Role,
    DateTimeOffset OccurredAt) : IDomainEvent;
