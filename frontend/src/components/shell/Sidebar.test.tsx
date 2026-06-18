import { render, screen } from '@testing-library/react';
import { Sidebar } from './Sidebar';
import { SessionProvider } from '@/context/SessionContext';

// Mock matchMedia for responsive hooks if needed
beforeAll(() => {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: jest.fn().mockImplementation((query) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: jest.fn(),
      removeListener: jest.fn(),
      addEventListener: jest.fn(),
      removeEventListener: jest.fn(),
      dispatchEvent: jest.fn(),
    })),
  });
});

describe('Sidebar', () => {
  it('Sidebar_AsReceptionist_DoesNotShowFinanceLink', () => {
    const mockSession = {
      role: 'Receptionist',
      tenantId: 'cfc_demo',
      userId: 'user-123',
      expiresAt: new Date(),
    };

    render(
      <SessionProvider session={mockSession}>
        <Sidebar />
      </SessionProvider>
    );

    // Should see allowed links
    expect(screen.getByText('Agenda')).toBeInTheDocument();
    expect(screen.getByText('Alunos')).toBeInTheDocument();
    expect(screen.getByText('Contratos')).toBeInTheDocument();
    expect(screen.getByText('Conformidade')).toBeInTheDocument();

    // Should NOT see unauthorized links
    expect(screen.queryByText('Financeiro')).not.toBeInTheDocument();
    expect(screen.queryByText('Usuários')).not.toBeInTheDocument();
  });
});
