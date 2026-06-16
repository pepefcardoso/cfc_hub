using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Identity.Commands.DeactivateStaffUser;

public class DeactivateStaffUserCommandHandler : IRequestHandler<DeactivateStaffUserCommand, Result<Unit>>
{
    private readonly IStaffUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeactivateStaffUserCommandHandler(
        IStaffUserRepository userRepository,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Unit>> Handle(DeactivateStaffUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != RoleType.Admin)
        {
            throw new ForbiddenException("Apenas administradores podem desativar usuários.", "FORBIDDEN");
        }

        var userId = new StaffUserId(request.StaffUserId);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        
        if (user == null)
        {
            throw new NotFoundException("Usuário não encontrado.", "USER_NOT_FOUND");
        }

        user.Deactivate();

        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
