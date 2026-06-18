import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { BookSlotDialog } from './BookSlotDialog';
import { schedulingApi } from '@/lib/api/scheduling';
import { useStudents } from '@/hooks/useStudents';

// Mock dependencies
vi.mock('@/lib/api/scheduling', () => ({
  schedulingApi: {
    bookSlot: vi.fn(),
  },
}));

vi.mock('@/hooks/useStudents', () => ({
  useStudents: vi.fn(),
}));

describe('BookSlotDialog', () => {
  const mockSlot = {
    id: 'slot-1',
    date: '2026-06-18',
    startTime: '10:00',
    endTime: '10:50',
    instructorId: 'inst-1',
    instructorName: 'João Instrutor',
    vehicleId: 'CAR-1234',
    trackType: 'Pista Principal',
    status: 'available' as const,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (useStudents as any).mockReturnValue({
      students: [{ id: 'stu-1', name: 'Maria Silva', cpf: '123.456.789-00' }],
      isLoading: false,
    });
  });

  it('BookSlotDialog_OnConflict_ShowsInlineErrorWithoutClosing', async () => {
    // Arrange
    const handleClose = vi.fn();
    const handleSuccess = vi.fn();

    // Mock API to return 409 Conflict
    (schedulingApi.bookSlot as any).mockRejectedValue({
      status: 409,
      type: 'Conflict',
      detail: 'Horário indisponível. Tente outro.',
    });

    render(
      <BookSlotDialog
        slot={mockSlot}
        isOpen={true}
        onClose={handleClose}
        onSuccess={handleSuccess}
        category="B"
      />
    );

    // Select student
    const studentButton = screen.getByText(/Maria Silva/i);
    fireEvent.click(studentButton);

    // Click confirm button
    const confirmButton = screen.getByRole('button', { name: /Confirmar Agendamento/i });
    fireEvent.click(confirmButton);

    // Assert API was called
    await waitFor(() => {
      expect(schedulingApi.bookSlot).toHaveBeenCalledWith({
        slotId: 'slot-1',
        date: '2026-06-18',
        startTime: '10:00',
        studentId: 'stu-1',
        category: 'B',
        instructorId: 'inst-1',
      });
    });

    // Assert inline error is displayed
    const errorMessage = await screen.findByText('Horário indisponível. Tente outro.');
    expect(errorMessage).toBeInTheDocument();

    // Assert dialog did not close
    expect(handleClose).not.toHaveBeenCalled();
    expect(handleSuccess).not.toHaveBeenCalled();
  });
});
