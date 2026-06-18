import React from 'react';
import { getSession } from '@/lib/auth/session';
import { redirect } from 'next/navigation';
import { SessionProvider } from '@/context/SessionContext';
import { Sidebar } from '@/components/shell/Sidebar';
import { Topbar } from '@/components/shell/Topbar';

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = getSession();

  if (!session) {
    redirect('/login');
  }

  return (
    <SessionProvider session={session}>
      <div className="flex h-screen overflow-hidden bg-background">
        <Sidebar />
        <div className="flex flex-col flex-1 overflow-hidden">
          <Topbar />
          <main className="flex-1 overflow-y-auto p-4 md:p-6">
            {children}
          </main>
        </div>
      </div>
    </SessionProvider>
  );
}
