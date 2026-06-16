using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using MediatR;

namespace CFCHub.Application.Identity.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private readonly IStaffUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IStaffSessionCacheService _sessionCacheService;
    private readonly ITenantContext _tenantContext;
    private readonly ISystemClock _clock;

    public LoginCommandHandler(
        IStaffUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IStaffSessionCacheService sessionCacheService,
        ITenantContext tenantContext,
        ISystemClock clock)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _sessionCacheService = sessionCacheService;
        _tenantContext = tenantContext;
        _clock = clock;
    }

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedException("Credenciais inválidas.", "UNAUTHORIZED");
        }

        var isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Credenciais inválidas.", "UNAUTHORIZED");
        }

        if (user.Status != StaffUserStatus.Active)
        {
            throw new ForbiddenException("A conta não está ativa.", "ACCOUNT_INACTIVE");
        }

        user.RecordLogin(_clock);
        await _userRepository.UpdateAsync(user, cancellationToken);

        var tokenResult = _jwtTokenService.GenerateToken(user, _tenantContext);

        await _sessionCacheService.CacheSessionAsync(tokenResult.Jti, TimeSpan.FromSeconds(3600), cancellationToken);

        return new LoginResult(
            AccessToken: tokenResult.AccessToken,
            ExpiresAt: tokenResult.ExpiresAt,
            StaffUserId: user.Id.Value,
            Role: user.Role
        );
    }
}
