'use client';

import React, { useEffect, useState } from 'react';
import { useSession } from '@/context/SessionContext';
import { NavItem } from './NavItem';
import { 
  CalendarDays, 
  Users, 
  FileText, 
  DollarSign, 
  ShieldCheck, 
  Settings,
  Menu,
  X,
  ChevronLeft,
  ChevronRight
} from 'lucide-react';
import { cn } from '@/lib/utils';
import Link from 'next/link';
import { checkRouteAccess } from '@/lib/permissions';

interface RouteItem {
  href: string;
  label: string;
  icon: React.ReactNode;
}

const routes: RouteItem[] = [
  { href: '/agenda', label: 'Agenda', icon: <CalendarDays className="w-5 h-5" /> },
  { href: '/alunos', label: 'Alunos', icon: <Users className="w-5 h-5" /> },
  { href: '/contratos', label: 'Contratos', icon: <FileText className="w-5 h-5" /> },
  { href: '/financeiro', label: 'Financeiro', icon: <DollarSign className="w-5 h-5" /> },
  { href: '/conformidade', label: 'Conformidade', icon: <ShieldCheck className="w-5 h-5" /> },
  { href: '/configuracoes/usuarios', label: 'Usuários', icon: <Settings className="w-5 h-5" /> },
];

export function Sidebar() {
  const { role } = useSession();
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const [isMounted, setIsMounted] = useState(false);

  useEffect(() => {
    setIsMounted(true);
    const saved = localStorage.getItem('sidebar_collapsed');
    if (saved) {
      setIsCollapsed(saved === 'true');
    }

    const openHandler = () => setIsMobileOpen(true);
    const toggleHandler = () => setIsMobileOpen(prev => !prev);
    
    document.addEventListener('openMobileSidebar', openHandler);
    document.addEventListener('toggleMobileSidebar', toggleHandler);
    
    return () => {
      document.removeEventListener('openMobileSidebar', openHandler);
      document.removeEventListener('toggleMobileSidebar', toggleHandler);
    };
  }, []);

  const toggleCollapse = () => {
    const newVal = !isCollapsed;
    setIsCollapsed(newVal);
    localStorage.setItem('sidebar_collapsed', String(newVal));
  };

  const closeMobile = () => setIsMobileOpen(false);

  // Filter routes based on role using permissions.ts
  const visibleRoutes = routes.filter(route => 
    role && checkRouteAccess(role, route.href)
  );

  if (!isMounted) {
    return null; // Avoid hydration mismatch on initial render for localStorage
  }

  return (
    <>
      {/* Mobile overlay */}
      {isMobileOpen && (
        <div 
          className="fixed inset-0 z-40 bg-black/50 md:hidden"
          onClick={closeMobile}
          aria-hidden="true"
        />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-50 flex flex-col bg-card border-r transition-all duration-300 ease-in-out md:relative",
          isMobileOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0",
          isCollapsed ? "md:w-16" : "md:w-64",
          "w-64" // Mobile width is always 64
        )}
      >
        <div className="flex h-16 shrink-0 items-center justify-between px-4 border-b">
          <Link href="/" className={cn("flex items-center gap-2 font-bold text-lg text-primary outline-none focus-visible:ring-2 focus-visible:ring-ring rounded", isCollapsed ? "md:hidden" : "")} onClick={closeMobile}>
            <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-primary-foreground">
              C
            </div>
            <span>CFCHub</span>
          </Link>
          
          {/* Logo when collapsed on desktop */}
          {isCollapsed && (
            <Link href="/" className="hidden md:flex h-8 w-8 items-center justify-center rounded-md bg-primary text-primary-foreground font-bold outline-none focus-visible:ring-2 focus-visible:ring-ring mx-auto">
              C
            </Link>
          )}

          {/* Mobile close button */}
          <button 
            className="md:hidden p-1 rounded-md text-muted-foreground hover:bg-muted outline-none focus-visible:ring-2 focus-visible:ring-ring"
            onClick={closeMobile}
            aria-label="Close sidebar"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <nav className="flex-1 overflow-y-auto p-2 space-y-1">
          {visibleRoutes.map((route) => (
            <NavItem
              key={route.href}
              href={route.href}
              label={route.label}
              icon={route.icon}
              isCollapsed={isCollapsed}
              onClick={closeMobile}
            />
          ))}
        </nav>

        {/* Desktop collapse toggle */}
        <div className="hidden md:flex p-2 border-t">
          <button
            onClick={toggleCollapse}
            className="flex w-full items-center justify-center rounded-md p-2 text-muted-foreground hover:bg-muted hover:text-foreground outline-none focus-visible:ring-2 focus-visible:ring-ring"
            aria-label={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
          >
            {isCollapsed ? <ChevronRight className="w-5 h-5" /> : <ChevronLeft className="w-5 h-5" />}
          </button>
        </div>
      </aside>
    </>
  );
}
