using System;
using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Identity.Events;

public record StaffUserRoleChangedEvent(
    StaffUserId StaffUserId,
    RoleType OldRole,
    RoleType NewRole,
    DateTimeOffset OccurredAt) : IDomainEvent;
