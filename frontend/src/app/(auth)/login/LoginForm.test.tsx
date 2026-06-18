import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import LoginForm from './LoginForm';
import { server } from '@/tests/mocks/server';
import { http, HttpResponse } from 'msw';

// Mock Next.js router
vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: vi.fn(),
    refresh: vi.fn(),
  }),
  useSearchParams: () => ({
    get: vi.fn(),
  }),
}));

describe('LoginForm', () => {
  it('LoginForm_SubmitWithInvalidEmail_ShowsValidationError', async () => {
    server.use(
      http.post('/api/auth/login', () => {
        return HttpResponse.json({}, { status: 401 });
      })
    );

    const user = userEvent.setup();
    render(<LoginForm />);

    const emailInput = screen.getByLabelText(/E-mail/i);
    const passwordInput = screen.getByLabelText(/Senha/i);
    const submitButton = screen.getByRole('button', { name: /Entrar/i });

    await user.type(emailInput, 'invalid-email');
    await user.type(passwordInput, 'validpassword123');
    
    // Fire submit directly on the form to bypass native HTML5 validation blocking
    fireEvent.submit(emailInput.closest('form')!);

    await waitFor(() => {
      expect(screen.getByText('E-mail inválido.')).toBeInTheDocument();
    });
  });
});
