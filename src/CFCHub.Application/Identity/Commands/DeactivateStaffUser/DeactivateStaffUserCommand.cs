using System;
using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Identity.Commands.DeactivateStaffUser;

public record DeactivateStaffUserCommand(
    Guid StaffUserId) : IRequest<Result<Unit>>;
