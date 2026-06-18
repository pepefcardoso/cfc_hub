import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { SlotActions } from './SlotActions';
import { SessionProvider } from '@/context/SessionContext';

import userEvent from '@testing-library/user-event';

const mockSlot = {
  id: '1',
  date: '2099-01-01',
  startTime: '10:00',
  endTime: '11:00',
  status: 'Confirmed' as const,
  studentId: 'student-1'
};

const adminSession = {
  role: 'Admin',
  tenantId: 'tenant-1',
  userId: 'admin-1',
  expiresAt: new Date(),
  accessToken: 'token',
};

const adminWrapper = ({ children }: { children: React.ReactNode }) => (
  <SessionProvider session={adminSession}>{children}</SessionProvider>
);

const mockOnStatusChange = vi.fn();

describe('SlotActions Component', () => {
  it('SlotActions_AsStudent_OnlyShowsCancelForOwnConfirmedSlot', () => {
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

    // Should not show Complete or Falta
    expect(screen.queryByText('Concluir')).not.toBeInTheDocument();
    expect(screen.queryByText('Falta')).not.toBeInTheDocument();
    
    // Should show Cancel
    expect(screen.getByText('Cancelar')).toBeInTheDocument();
  });

  it('SlotActions_AsAdmin_CanCompleteAndNoShow', async () => {
    render(<SlotActions slot={mockSlot} onStatusChange={mockOnStatusChange} />, { wrapper: adminWrapper });
    
    await userEvent.click(screen.getByRole('button', { name: /concluir/i }));
    expect(mockOnStatusChange).toHaveBeenCalledWith('1', 'Completed');

    await userEvent.click(screen.getByRole('button', { name: /falta/i }));
    expect(mockOnStatusChange).toHaveBeenCalledWith('1', 'NoShow');
  });

  it('SlotActions_HandleCancel_ValidatesReasonAndCancels', async () => {
    render(<SlotActions slot={mockSlot} onStatusChange={mockOnStatusChange} />, { wrapper: adminWrapper });
    
    await userEvent.click(screen.getByRole('button', { name: /cancelar/i }));
    const textarea = screen.getByPlaceholderText(/motivo do cancelamento/i);
    
    await userEvent.type(textarea, 'short');
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    expect(mockOnStatusChange).not.toHaveBeenCalled();

    await userEvent.clear(textarea);
    await userEvent.type(textarea, 'long enough reason');
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    expect(mockOnStatusChange).toHaveBeenCalledWith('1', 'Cancelled', 'long enough reason');
  });

  it('SlotActions_HandleErrors', async () => {
    const errorMock = vi.fn().mockRejectedValue(new Error('fail'));
    render(<SlotActions slot={mockSlot} onStatusChange={errorMock} />, { wrapper: adminWrapper });
    
    await userEvent.click(screen.getByRole('button', { name: /concluir/i }));
    expect(errorMock).toHaveBeenCalled();
    
    await userEvent.click(screen.getByRole('button', { name: /falta/i }));
    expect(errorMock).toHaveBeenCalledTimes(2);

    await userEvent.click(screen.getByRole('button', { name: /cancelar/i }));
    const textarea = screen.getByPlaceholderText(/motivo do cancelamento/i);
    await userEvent.type(textarea, 'long enough reason');
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }));
    expect(errorMock).toHaveBeenCalledTimes(3);
  });
});
