using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Application.Identity.Commands.Login;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Identity.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly IStaffUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IStaffSessionCacheService _sessionCacheService;
    private readonly ITenantContext _tenantContext;
    private readonly ISystemClock _clock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepository = Substitute.For<IStaffUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _sessionCacheService = Substitute.For<IStaffSessionCacheService>();
        _tenantContext = Substitute.For<ITenantContext>();
        _clock = Substitute.For<ISystemClock>();
        
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        _handler = new LoginCommandHandler(
            _userRepository,
            _passwordHasher,
            _jwtTokenService,
            _sessionCacheService,
            _tenantContext,
            _clock);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("test@cfchub.com.br", "wrong_password");
        var userId = new StaffUserId(Guid.NewGuid());
        var user = StaffUser.Create(userId, "Test User", "test@cfchub.com.br", "hashed_password", RoleType.Admin, _clock);

        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(false);

        // Act
        var action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        var command = new LoginCommand("unknown@cfchub.com.br", "password");

        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns((StaffUser?)null);

        // Act
        var action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Credenciais inválidas.");
    }

    [Fact]
    public async Task Handle_WithInactiveUser_ReturnsForbidden()
    {
        // Arrange
        var command = new LoginCommand("test@cfchub.com.br", "correct_password");
        var userId = new StaffUserId(Guid.NewGuid());
        var user = StaffUser.Create(userId, "Test User", "test@cfchub.com.br", "hashed_password", RoleType.Admin, _clock);
        user.Deactivate(); // Set to Inactive

        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);

        // Act
        var action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("A conta não está ativa.");
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsJwt()
    {
        // Arrange
        var command = new LoginCommand("test@cfchub.com.br", "correct_password");
        var userId = new StaffUserId(Guid.NewGuid());
        var user = StaffUser.Create(userId, "Test User", "test@cfchub.com.br", "hashed_password", RoleType.Admin, _clock);
        
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var tokenResult = new JwtTokenResult("valid_token", jti, expiresAt);

        _userRepository.GetByEmailAsync(command.Email, Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(command.Password, user.PasswordHash).Returns(true);
        _jwtTokenService.GenerateToken(user, _tenantContext).Returns(tokenResult);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().Be("valid_token");
        result.Value.ExpiresAt.Should().Be(expiresAt);
        result.Value.StaffUserId.Should().Be(userId.Value);
        result.Value.Role.Should().Be(RoleType.Admin);

        // Ensure user login was recorded
        await _userRepository.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
        
        // Ensure session cache was created with 3600s TTL
        await _sessionCacheService.Received(1).CacheSessionAsync(jti, TimeSpan.FromSeconds(3600), Arg.Any<CancellationToken>());
    }
}
