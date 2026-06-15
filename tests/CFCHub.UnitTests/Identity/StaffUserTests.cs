using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Xunit;
using CFCHub.Domain.Identity;
using CFCHub.Domain.Identity.Events;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;

namespace CFCHub.UnitTests.Identity;

public class StaffUserTests
{
    [Fact]
    public void Create_WithValidData_RaisesCreatedEvent()
    {
        // Arrange
        var id = new StaffUserId(Guid.NewGuid());
        var name = "John Doe";
        var email = "john@example.com";
        var passwordHash = "hashed_password";
        var role = RoleType.Receptionist;
        var clock = Substitute.For<ISystemClock>();
        var now = DateTimeOffset.UtcNow;
        clock.UtcNow.Returns(now);

        // Act
        var user = StaffUser.Create(id, name, email, passwordHash, role, clock);

        // Assert
        user.Id.Should().Be(id);
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.Role.Should().Be(role);
        user.Status.Should().Be(StaffUserStatus.Active);

        var domainEvent = user.DomainEvents.SingleOrDefault(e => e is StaffUserCreatedEvent) as StaffUserCreatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.StaffUserId.Should().Be(id);
        domainEvent.Role.Should().Be(role);
        domainEvent.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void ChangeRole_WhenUserInactive_ThrowsUnprocessableException()
    {
        // Arrange
        var id = new StaffUserId(Guid.NewGuid());
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var user = StaffUser.Create(id, "John Doe", "john@example.com", "hash", RoleType.Receptionist, clock);
        user.Deactivate();
        user.ClearDomainEvents();

        // Act
        var act = () => user.ChangeRole(RoleType.Admin, clock);

        // Assert
        act.Should().Throw<UnprocessableException>()
            .WithMessage("Cannot change role of an inactive or locked user.");
        user.DomainEvents.Should().BeEmpty();
    }
    
    [Fact]
    public void ChangeRole_WhenUserActive_ChangesRoleAndRaisesEvent()
    {
        // Arrange
        var id = new StaffUserId(Guid.NewGuid());
        var clock = Substitute.For<ISystemClock>();
        var now = DateTimeOffset.UtcNow;
        clock.UtcNow.Returns(now);

        var user = StaffUser.Create(id, "John Doe", "john@example.com", "hash", RoleType.Receptionist, clock);
        user.ClearDomainEvents();

        // Act
        user.ChangeRole(RoleType.Admin, clock);

        // Assert
        user.Role.Should().Be(RoleType.Admin);
        var domainEvent = user.DomainEvents.SingleOrDefault(e => e is StaffUserRoleChangedEvent) as StaffUserRoleChangedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.StaffUserId.Should().Be(id);
        domainEvent.OldRole.Should().Be(RoleType.Receptionist);
        domainEvent.NewRole.Should().Be(RoleType.Admin);
        domainEvent.OccurredAt.Should().Be(now);
    }

    [Fact]
    public void RecordLogin_UpdatesLastLoginAt()
    {
        // Arrange
        var id = new StaffUserId(Guid.NewGuid());
        var clock = Substitute.For<ISystemClock>();
        var now = DateTimeOffset.UtcNow;
        clock.UtcNow.Returns(now);

        var user = StaffUser.Create(id, "John Doe", "john@example.com", "hash", RoleType.Receptionist, clock);
        
        var loginTime = now.AddDays(1);
        clock.UtcNow.Returns(loginTime);

        // Act
        user.RecordLogin(clock);

        // Assert
        user.LastLoginAt.Should().Be(loginTime);
    }
}
