'use client';

import React, { useState, useRef, useEffect } from 'react';
import { useSession } from '@/context/SessionContext';
import { useRouter } from 'next/navigation';
import { User, LogOut, ChevronDown } from 'lucide-react';

export function UserMenu() {
  const { role, userId } = useSession();
  const router = useRouter();
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // Close menu when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = async () => {
    try {
      await fetch('/api/auth/logout', { method: 'POST' });
    } catch (error) {
      console.error('Logout failed', error);
    } finally {
      router.push('/login');
      router.refresh();
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') setIsOpen(false);
  };

  const roleColors: Record<string, string> = {
    Admin: 'bg-red-100 text-red-800',
    Receptionist: 'bg-blue-100 text-blue-800',
    Financial: 'bg-green-100 text-green-800',
    Instructor: 'bg-purple-100 text-purple-800',
  };

  const roleColor = roleColors[role] || 'bg-gray-100 text-gray-800';

  return (
    <div className="relative" ref={menuRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        onKeyDown={handleKeyDown}
        className="flex items-center gap-2 p-2 rounded-md hover:bg-muted transition-colors outline-none focus-visible:ring-2 focus-visible:ring-ring"
        aria-haspopup="menu"
        aria-expanded={isOpen}
      >
        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-primary">
          <User className="h-4 w-4" />
        </div>
        <div className="hidden md:flex flex-col items-start text-sm">
          <span className="font-medium leading-none">Usuário {userId.substring(0, 4)}</span>
        </div>
        <ChevronDown className="h-4 w-4 text-muted-foreground" />
      </button>

      {isOpen && (
        <div 
          className="absolute right-0 mt-2 w-48 rounded-md border bg-card shadow-md z-50 py-1"
          role="menu"
          aria-orientation="vertical"
        >
          <div className="px-4 py-2 border-b">
            <p className="text-sm font-medium">Conta</p>
            <span className={`inline-block mt-1 px-2 py-0.5 text-xs font-semibold rounded-full ${roleColor}`}>
              {role}
            </span>
          </div>
          <div className="py-1">
            <button
              onClick={handleLogout}
              className="flex w-full items-center px-4 py-2 text-sm text-red-600 hover:bg-red-50 focus:bg-red-50 outline-none focus-visible:ring-2 focus-visible:ring-red-500"
              role="menuitem"
            >
              <LogOut className="mr-2 h-4 w-4" />
              Sair
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
