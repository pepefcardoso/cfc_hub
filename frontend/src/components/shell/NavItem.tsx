'use client';

import React from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '@/lib/utils';

export interface NavItemProps {
  href: string;
  label: string;
  icon: React.ReactNode;
  isCollapsed?: boolean;
  onClick?: () => void;
}

export function NavItem({ href, label, icon, isCollapsed, onClick }: NavItemProps) {
  const pathname = usePathname();
  const isActive = pathname === href || pathname.startsWith(`${href}/`);

  return (
    <Link
      href={href}
      onClick={onClick}
      title={isCollapsed ? label : undefined}
      className={cn(
        'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors outline-none focus-visible:ring-2 focus-visible:ring-ring',
        isActive
          ? 'bg-primary/10 text-primary hover:bg-primary/20'
          : 'text-muted-foreground hover:bg-muted hover:text-foreground',
        isCollapsed ? 'justify-center px-2' : 'justify-start'
      )}
    >
      <span className="flex items-center justify-center h-5 w-5 shrink-0">{icon}</span>
      {!isCollapsed && <span>{label}</span>}
    </Link>
  );
}
