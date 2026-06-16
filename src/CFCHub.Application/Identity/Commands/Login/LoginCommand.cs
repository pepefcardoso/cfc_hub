using CFCHub.Domain.Shared;
using MediatR;

namespace CFCHub.Application.Identity.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<LoginResult>>;
