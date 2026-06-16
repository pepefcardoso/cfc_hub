using System;
using CFCHub.Domain.Identity;

namespace CFCHub.Application.Identity.Queries.GetStaffUsers;

public record StaffUserResult(
    Guid Id,
    string Name,
    string? Email,
    RoleType Role,
    StaffUserStatus Status,
    DateTimeOffset? LastLoginAt);
