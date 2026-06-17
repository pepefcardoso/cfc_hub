import { cookies } from 'next/headers';
import { jwtDecode } from 'jwt-decode';

export const SESSION_COOKIE_NAME = 'session';

interface SessionPayload {
  role: string;
  tenant_id?: string;
  tenantId?: string;
  sub?: string;
  userId?: string;
  exp: number;
}

export interface Session {
  role: string;
  tenantId: string;
  userId: string;
  expiresAt: Date;
}

export function decodeSession(token: string): Session | null {
  try {
    const decoded = jwtDecode<SessionPayload>(token);
    
    if (decoded.exp * 1000 < Date.now()) {
      return null; // Expired
    }

    return {
      role: decoded.role,
      tenantId: decoded.tenant_id || decoded.tenantId || '',
      userId: decoded.sub || decoded.userId || '',
      expiresAt: new Date(decoded.exp * 1000),
    };
  } catch {
    return null;
  }
}

export function getSession(): Session | null {
  const cookieStore = cookies();
  const sessionCookie = cookieStore.get(SESSION_COOKIE_NAME);

  if (!sessionCookie || !sessionCookie.value) {
    return null;
  }

  return decodeSession(sessionCookie.value);
}

export function clearSession() {
  const cookieStore = cookies();
  cookieStore.delete(SESSION_COOKIE_NAME);
}
