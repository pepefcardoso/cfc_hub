import { ReactNode } from 'react';
import { useSession } from '@/context/SessionContext';
import { RoleType } from '@/lib/permissions';

interface RequireRoleProps {
  roles: RoleType[];
  children: ReactNode;
}

/**
 * Component that conditionally renders its children based on the current user's role.
 * 
 * @param roles An array of allowed roles. E.g., ['Admin', 'Financial']
 * @param children The content to render if the user has an allowed role
 * @returns The children if authorized, otherwise null
 */
export function RequireRole({ roles, children }: RequireRoleProps) {
  const { role } = useSession();

  if (!role) {
    return null;
  }

  if (roles.includes(role as RoleType)) {
    return <>{children}</>;
  }

  return null;
}
