using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Identity.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Identity.Commands.CreateStaffUser;

public class CreateStaffUserCommandHandler : IRequestHandler<CreateStaffUserCommand, Result<StaffUserId>>
{
    private readonly IStaffUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;

    public CreateStaffUserCommandHandler(
        IStaffUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        ISystemClock clock,
        IIdGenerator idGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _clock = clock;
        _idGenerator = idGenerator;
    }

    public async Task<Result<StaffUserId>> Handle(CreateStaffUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.Role != RoleType.Admin)
        {
            throw new ForbiddenException("Apenas administradores podem criar usuários.", "FORBIDDEN");
        }

        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new ConflictException("E-mail já está em uso.", "EMAIL_IN_USE");
        }

        var userId = _idGenerator.NewId<StaffUserId>();
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = StaffUser.Create(userId, request.Name, request.Email, passwordHash, request.Role, _clock);
        
        user.AddDomainEvent(new WelcomeEmailRequestedEvent(user.Id, request.Password, _clock.UtcNow));

        await _userRepository.AddAsync(user, cancellationToken);

        return Result<StaffUserId>.Success(user.Id);
    }
}
