export type RoleType = 'Admin' | 'Receptionist' | 'Instructor' | 'Financial';
export type RouteType = string;

interface RolePermissionsMap {
  allowedFields: string[];
  allowedRoutes: RouteType[];
}

export const RolePermissions: Record<RoleType, RolePermissionsMap> = {
  Admin: {
    allowedFields: ['*'],
    allowedRoutes: ['*'],
  },
  Receptionist: {
    allowedFields: [
      'Student.Name',
      'Student.Email',
      'Student.Phone',
    ],
    allowedRoutes: [
      '/agenda',
      '/alunos',
      '/contratos',
      '/conformidade',
    ],
  },
  Instructor: {
    allowedFields: [
      'Student.Name',
    ],
    allowedRoutes: [
      '/agenda',
      '/alunos',
    ],
  },
  Financial: {
    allowedFields: [
      'Financial.*',
    ],
    allowedRoutes: [
      '/agenda',
      '/alunos',
      '/financeiro',
    ],
  },
};

/**
 * Checks if a given role has access to a specific field.
 * Mimics CFCHub.Domain.Identity.FieldAccessPolicy
 */
export function checkFieldAccess(role: string, fieldName: string): boolean {
  if (role === 'Admin') return true;

  const roleData = RolePermissions[role as RoleType];
  if (!roleData) return false;

  // Explicit allow check
  if (roleData.allowedFields.includes(fieldName)) return true;

  // Prefix allow check (e.g. Financial.*)
  const parts = fieldName.split('.');
  if (parts.length > 1) {
    if (roleData.allowedFields.includes(`${parts[0]}.*`)) return true;
  }

  return false;
}

/**
 * Checks if a given role has access to a specific route.
 */
export function checkRouteAccess(role: string, route: RouteType): boolean {
  if (role === 'Admin') return true;

  const roleData = RolePermissions[role as RoleType];
  if (!roleData) return false;

  if (roleData.allowedRoutes.includes('*')) return true;
  
  // Basic route matching logic
  return roleData.allowedRoutes.some(allowedRoute => route.startsWith(allowedRoute));
}
