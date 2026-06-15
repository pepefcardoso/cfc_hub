using System.Collections.Generic;

namespace CFCHub.Domain.Identity;

public class Role
{
    public RoleType Type { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public IReadOnlyList<PermissionType> Permissions { get; private set; }

    public Role(RoleType type, string name, string description, IReadOnlyList<PermissionType> permissions)
    {
        Type = type;
        Name = name;
        Description = description;
        Permissions = permissions;
    }
}
