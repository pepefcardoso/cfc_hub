using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Identity.Commands.CreateStaffUser;

public record CreateStaffUserCommand(
    string Name,
    string Email,
    string Password,
    RoleType Role) : IRequest<Result<StaffUserId>>;
