'use client';

import React, { createContext, useContext, ReactNode } from 'react';
import { Session } from '@/lib/auth/session';

interface SessionContextType {
  session: Session;
  role: string;
  tenantId: string;
  userId: string;
}

const SessionContext = createContext<SessionContextType | undefined>(undefined);

interface SessionProviderProps {
  children: ReactNode;
  session: Session;
}

export function SessionProvider({ children, session }: SessionProviderProps) {
  return (
    <SessionContext.Provider
      value={{
        session,
        role: session.role,
        tenantId: session.tenantId,
        userId: session.userId,
      }}
    >
      {children}
    </SessionContext.Provider>
  );
}

export function useSession(): SessionContextType {
  const context = useContext(SessionContext);
  if (context === undefined) {
    throw new Error('useSession must be used within a SessionProvider');
  }
  return context;
}
