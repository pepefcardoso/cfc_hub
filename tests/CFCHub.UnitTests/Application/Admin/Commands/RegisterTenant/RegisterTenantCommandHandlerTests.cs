using System;
using System.Threading;
using System.Threading.Tasks;
using CFCHub.Application.Admin.Commands.RegisterTenant;
using CFCHub.Application.Common.Interfaces;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Shared.Exceptions;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CFCHub.UnitTests.Application.Admin.Commands.RegisterTenant;

public class RegisterTenantCommandHandlerTests
{
    private readonly ITenantRegistry _tenantRegistryMock;
    private readonly ITenantProvisioningService _provisioningServiceMock;
    private readonly ICurrentUserService _currentUserServiceMock;
    private readonly RegisterTenantCommandHandler _handler;

    public RegisterTenantCommandHandlerTests()
    {
        _tenantRegistryMock = Substitute.For<ITenantRegistry>();
        _provisioningServiceMock = Substitute.For<ITenantProvisioningService>();
        _currentUserServiceMock = Substitute.For<ICurrentUserService>();

        _handler = new RegisterTenantCommandHandler(
            _tenantRegistryMock,
            _provisioningServiceMock,
            _currentUserServiceMock);
    }

    [Fact]
    public async Task Handle_WithNonDetranAdminRole_ShouldThrowForbiddenException()
    {
        // Arrange
        _currentUserServiceMock.Role.Returns(RoleType.Admin);
        var command = new RegisterTenantCommand("Test CFC", "test_cfc", "test@test.com", "12345678901234");

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Apenas o super-administrador pode registrar novos tenants.");
    }

    [Fact]
    public async Task Handle_WithExistingSlug_ShouldThrowConflictException()
    {
        // Arrange
        _currentUserServiceMock.Role.Returns(RoleType.DetranAdmin);
        var command = new RegisterTenantCommand("Test CFC", "test_cfc", "test@test.com", "12345678901234");
        
        _tenantRegistryMock.IsSlugUniqueAsync("test_cfc", Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("O slug 'test_cfc' já está em uso.");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldProvisionTenantAndReturnResult()
    {
        // Arrange
        _currentUserServiceMock.Role.Returns(RoleType.DetranAdmin);
        var command = new RegisterTenantCommand("Test CFC", "test_cfc", "test@test.com", "12345678901234");
        
        _tenantRegistryMock.IsSlugUniqueAsync("test_cfc", Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().NotBeEmpty();
        result.SchemaName.Should().Be("cfc_test_cfc");
        result.Slug.Should().Be("test_cfc");

        await _provisioningServiceMock.Received(1)
            .ProvisionAsync("test_cfc", result.TenantId, Arg.Any<CancellationToken>());
    }
}
