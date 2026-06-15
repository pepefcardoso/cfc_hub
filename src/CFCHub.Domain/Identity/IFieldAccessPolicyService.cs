namespace CFCHub.Domain.Identity;

public interface IFieldAccessPolicyService
{
    FieldAccess CheckAccess(RoleType role, string fieldName);
}
