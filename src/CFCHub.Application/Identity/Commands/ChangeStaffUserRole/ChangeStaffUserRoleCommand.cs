using System;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Identity.Commands.ChangeStaffUserRole;

public record ChangeStaffUserRoleCommand(
    Guid StaffUserId,
    RoleType NewRole) : IRequest<Result<Unit>>;
