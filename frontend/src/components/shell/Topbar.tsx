'use client';

import React from 'react';
import { Menu } from 'lucide-react';
import { UserMenu } from './UserMenu';
import { useSession } from '@/context/SessionContext';

export function Topbar() {
  const { tenantId } = useSession();

  const handleOpenMobileSidebar = () => {
    document.dispatchEvent(new Event('openMobileSidebar'));
  };

  return (
    <header className="flex h-16 shrink-0 items-center justify-between border-b bg-card px-4 md:px-6">
      <div className="flex items-center gap-4">
        {/* Mobile menu trigger */}
        <button
          onClick={handleOpenMobileSidebar}
          className="md:hidden p-2 -ml-2 rounded-md text-muted-foreground hover:bg-muted hover:text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label="Open mobile menu"
        >
          <Menu className="h-5 w-5" />
        </button>

        {/* Tenant context */}
        <div className="flex items-center">
          <span className="text-sm font-semibold text-muted-foreground uppercase tracking-wider">
            {tenantId ? tenantId.replace('cfc_', '') : 'TENANT'}
          </span>
        </div>
      </div>

      <div className="flex items-center gap-4">
        <UserMenu />
      </div>
    </header>
  );
}
