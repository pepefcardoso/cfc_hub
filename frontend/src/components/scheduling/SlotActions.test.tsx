import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { SlotActions } from './SlotActions';
import { SessionProvider } from '@/context/SessionContext';

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(), // Deprecated
    removeListener: vi.fn(), // Deprecated
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

const mockSlot = {
  id: 'slot-1',
  date: '2026-06-18',
  startTime: '10:00',
  endTime: '11:00',
  status: 'Confirmed' as const,
};

describe('SlotActions', () => {
  it('SlotActions_AsStudent_DoesNotRenderCompleteButton', () => {
    const studentSession = {
      role: 'Student',
      tenantId: 'tenant-1',
      userId: 'student-1',
      expiresAt: new Date(),
      accessToken: 'token',
    };

    render(
      <SessionProvider session={studentSession}>
        <SlotActions slot={mockSlot} onStatusChange={vi.fn()} />
      </SessionProvider>
    );

    expect(screen.queryByText('Concluir')).not.toBeInTheDocument();
    expect(screen.queryByText('Falta')).not.toBeInTheDocument();
    expect(screen.getByText('Cancelar')).toBeInTheDocument();
  });
});
