namespace CFCHub.Domain.Identity;

public class Permission
{
    public PermissionType Type { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    public Permission(PermissionType type, string name, string description)
    {
        Type = type;
        Name = name;
        Description = description;
    }
}
