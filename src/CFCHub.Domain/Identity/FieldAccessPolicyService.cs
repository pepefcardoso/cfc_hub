namespace CFCHub.Domain.Identity;

public class FieldAccessPolicyService : IFieldAccessPolicyService
{
    private readonly FieldAccessPolicy _policy;

    public FieldAccessPolicyService()
    {
        _policy = FieldAccessPolicy.CreateDefault();
    }

    public FieldAccess CheckAccess(RoleType role, string fieldName)
    {
        return _policy.Check(role, fieldName);
    }
}
