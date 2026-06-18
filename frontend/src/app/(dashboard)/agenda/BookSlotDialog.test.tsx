import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { BookSlotDialog } from './BookSlotDialog';
import { server } from '@/tests/mocks/server';
import { http, HttpResponse } from 'msw';
import { Slot } from '@/lib/api/scheduling';

// Mock useStudents hook to avoid complex fetch setups
vi.mock('@/hooks/useStudents', () => ({
  useStudents: () => ({
    students: [
      { id: 'student-1', name: 'John Doe', cpf: '123.456.789-00' }
    ],
    isLoading: false,
  })
}));

describe('BookSlotDialog Component', () => {
  const mockSlot: Slot = {
    id: 'slot-1',
    date: '2026-06-18',
    startTime: '10:00',
    endTime: '11:00',
    status: 'Available',
    instructorId: 'inst-1',
    instructorName: 'Instructor Name',
  };

  it('BookSlotDialog_OnConflict409_ShowsInlineError', async () => {
    const user = userEvent.setup();

    // Mock the schedulingApi.bookSlot endpoint returning 409
    server.use(
      http.post('http://localhost:5000/api/v1/scheduling/slots', () => {
        return HttpResponse.json({
          status: 409,
          detail: 'Horário indisponível.',
        }, { status: 409 });
      })
    );

    render(
      <BookSlotDialog
        slot={mockSlot}
        isOpen={true}
        onClose={vi.fn()}
        onSuccess={vi.fn()}
        category="B"
      />
    );

    // Select the student from the mocked list
    const studentButton = screen.getByText(/John Doe/i);
    await user.click(studentButton);

    // Click confirm
    const confirmButton = screen.getByRole('button', { name: /Confirmar Agendamento/i });
    await user.click(confirmButton);

    // Wait for the conflict error to appear
    await waitFor(() => {
      expect(screen.getByText('Horário indisponível. Tente outro.')).toBeInTheDocument();
    });
  });
});
