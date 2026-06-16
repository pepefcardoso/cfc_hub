using System;
using CFCHub.Domain.Identity;

namespace CFCHub.Application.Identity.Commands.Login;

public record LoginResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid StaffUserId,
    RoleType Role
);
