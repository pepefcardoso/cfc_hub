import type { Session } from './types';

let sessionCache: Session | null = null;

export async function login(email: string, password: string): Promise<void> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => ({}));
    throw new Error(errorData.detail || 'Login failed');
  }

  sessionCache = await response.json();
}

export async function logout(): Promise<void> {
  await fetch('/api/auth/logout', { method: 'POST' });
  sessionCache = null;
  if (typeof window !== 'undefined') {
    window.location.href = '/login';
  }
}

export async function getSession(): Promise<Session | null> {
  if (sessionCache) {
    const expiresAt = new Date(sessionCache.expiresAt);
    if (expiresAt > new Date()) {
      return sessionCache;
    }
  }

  try {
    const response = await fetch('/api/auth/session');
    if (response.ok) {
      sessionCache = await response.json();
      return sessionCache;
    }
  } catch {
    // Return null on error
  }
  
  sessionCache = null;
  return null;
}

export function clearSessionCache(): void {
  sessionCache = null;
}
