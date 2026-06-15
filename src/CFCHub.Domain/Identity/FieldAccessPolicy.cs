using CFCHub.Domain.Shared;
using System.Collections.Generic;
using System.Linq;

namespace CFCHub.Domain.Identity;

public sealed class FieldAccessPolicy : ValueObject
{
    private readonly Dictionary<(RoleType, string), FieldAccess> _rules;

    private FieldAccessPolicy(Dictionary<(RoleType, string), FieldAccess> rules)
    {
        _rules = rules;
    }

    public static FieldAccessPolicy CreateDefault()
    {
        var rules = new Dictionary<(RoleType, string), FieldAccess>
        {
            // Receptionist
            { (RoleType.Receptionist, "Student.Name"), FieldAccess.Allowed },
            { (RoleType.Receptionist, "Student.Email"), FieldAccess.Allowed },
            { (RoleType.Receptionist, "Student.Phone"), FieldAccess.Allowed },
            { (RoleType.Receptionist, "Student.Cpf"), FieldAccess.Denied },
            { (RoleType.Receptionist, "Student.Rg"), FieldAccess.Denied },
            { (RoleType.Receptionist, "MedicalExam.*"), FieldAccess.Denied },
            
            // Instructor
            { (RoleType.Instructor, "Student.Name"), FieldAccess.Allowed },
            { (RoleType.Instructor, "MedicalExam.*"), FieldAccess.Denied },
            { (RoleType.Instructor, "Financial.*"), FieldAccess.Denied },
            
            // Financial
            { (RoleType.Financial, "Financial.*"), FieldAccess.Allowed },
            { (RoleType.Financial, "MedicalExam.*"), FieldAccess.Denied },
        };

        return new FieldAccessPolicy(rules);
    }

    public FieldAccess Check(RoleType role, string fieldName)
    {
        if (role == RoleType.Admin)
        {
            return FieldAccess.Allowed;
        }

        if (_rules.TryGetValue((role, fieldName), out var access))
        {
            return access;
        }

        var parts = fieldName.Split('.');
        if (parts.Length > 1)
        {
            if (_rules.TryGetValue((role, $"{parts[0]}.*"), out var prefixAccess))
            {
                return prefixAccess;
            }
        }

        // If not explicitly allowed, default to denied for sensitive roles
        // We will default to Denied to be safe.
        return FieldAccess.Denied;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        // Sort the rules so order doesn't matter for equality
        foreach (var rule in _rules.OrderBy(kvp => kvp.Key.Item1).ThenBy(kvp => kvp.Key.Item2))
        {
            yield return rule.Key.Item1;
            yield return rule.Key.Item2;
            yield return rule.Value;
        }
    }
}
