using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Application.Common.Security;
using CFCHub.Application.Identity.Commands.CreateStaffUser;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Identity.Commands.CreateStaffUser;

public class CreateStaffUserCommandHandlerTests
{
    private readonly IStaffUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _clock;
    private readonly IIdGenerator _idGenerator;
    private readonly CreateStaffUserCommandHandler _handler;

    public CreateStaffUserCommandHandlerTests()
    {
        _userRepository = Substitute.For<IStaffUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _clock = Substitute.For<ISystemClock>();
        _idGenerator = Substitute.For<IIdGenerator>();

        _handler = new CreateStaffUserCommandHandler(
            _userRepository,
            _passwordHasher,
            _currentUserService,
            _clock,
            _idGenerator);
    }

    [Fact]
    public async Task CreateStaffUser_WhenNotAdmin_ThrowsForbidden()
    {
        // Arrange
        var command = new CreateStaffUserCommand("John Doe", "john@example.com", "Password@1234", RoleType.Instructor);
        _currentUserService.Role.Returns(RoleType.Receptionist);

        // Act
        var action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Apenas administradores podem criar usuários.");
    }

    [Fact]
    public async Task CreateStaffUser_WithDuplicateEmail_ThrowsConflict()
    {
        // Arrange
        var command = new CreateStaffUserCommand("John Doe", "john@example.com", "Password@1234", RoleType.Instructor);
        _currentUserService.Role.Returns(RoleType.Admin);
        
        var existingUserId = new StaffUserId(Guid.NewGuid());
        var existingUser = StaffUser.Create(existingUserId, "Jane", "john@example.com", "hash", RoleType.Admin, _clock);
        
        _userRepository.GetByEmailAsync("john@example.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        // Act
        var action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("E-mail já está em uso.");
    }
}
