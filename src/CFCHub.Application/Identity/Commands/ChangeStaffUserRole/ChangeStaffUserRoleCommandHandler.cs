using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Identity.Commands.ChangeStaffUserRole;

public class ChangeStaffUserRoleCommandHandler : IRequestHandler<ChangeStaffUserRoleCommand, Result<Unit>>
{
    private readonly IStaffUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _clock;

    public ChangeStaffUserRoleCommandHandler(
        IStaffUserRepository userRepository,
        ICurrentUserService currentUserService,
        ISystemClock clock)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _clock = clock;
    }

    public async Task<Result<Unit>> Handle(ChangeStaffUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != RoleType.Admin)
        {
            throw new ForbiddenException("Apenas administradores podem alterar papéis.", "FORBIDDEN");
        }

        var userId = new StaffUserId(request.StaffUserId);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        
        if (user == null)
        {
            throw new NotFoundException("Usuário não encontrado.", "USER_NOT_FOUND");
        }

        user.ChangeRole(request.NewRole, _clock);

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
