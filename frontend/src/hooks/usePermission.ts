import { useSession } from '@/context/SessionContext';
import { checkFieldAccess } from '@/lib/permissions';

/**
 * Hook to evaluate whether the current user has access to a specific field.
 * Uses permissions.ts to mirror backend FieldAccessPolicy.
 *
 * @param field The field name to check, e.g., 'Student.Cpf'
 * @returns boolean true if the user's role allows access, false otherwise
 */
export function usePermission(field: string): boolean {
  const { role } = useSession();
  
  if (!role) {
    return false;
  }

  return checkFieldAccess(role, field);
}
