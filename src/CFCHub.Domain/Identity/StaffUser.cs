using System;
using CFCHub.Domain.Shared;
using CFCHub.Domain.Shared.Exceptions;
using CFCHub.Domain.Identity.Events;

namespace CFCHub.Domain.Identity;

public class StaffUser : AggregateRoot<StaffUserId>
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public RoleType Role { get; private set; }
    public StaffUserStatus Status { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private StaffUser(StaffUserId id, string name, string email, string passwordHash, RoleType role, StaffUserStatus status)
        : base(id)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        Status = status;
    }

#pragma warning disable CS8618 // Required by EF Core
    private StaffUser() : base() { }
#pragma warning restore CS8618

    public static StaffUser Create(
        StaffUserId id,
        string name,
        string email,
        string passwordHash,
        RoleType role,
        ISystemClock clock)
    {
        var user = new StaffUser(id, name, email, passwordHash, role, StaffUserStatus.Active);
        user.AddDomainEvent(new StaffUserCreatedEvent(user.Id, user.Role, clock.UtcNow));
        return user;
    }

    public void ChangeRole(RoleType newRole, ISystemClock clock)
    {
        if (Status != StaffUserStatus.Active)
        {
            throw new UnprocessableException("Cannot change role of an inactive or locked user.");
        }

        var oldRole = Role;
        Role = newRole;

        AddDomainEvent(new StaffUserRoleChangedEvent(Id, oldRole, newRole, clock.UtcNow));
    }

    public void RecordLogin(ISystemClock clock)
    {
        LastLoginAt = clock.UtcNow;
    }

    public void Deactivate()
    {
        Status = StaffUserStatus.Inactive;
    }
}
