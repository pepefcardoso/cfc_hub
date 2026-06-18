import { render, screen } from '@testing-library/react';
import { RequireRole } from './RequireRole';
import { SessionProvider } from '@/context/SessionContext';

describe('RequireRole Component', () => {
  it('RequireRole_AsInstructor_DoesNotRenderAdminContent', () => {
    const mockSession = {
      role: 'Instructor',
      tenantId: 'cfc_demo',
      userId: 'user-123',
      expiresAt: new Date(),
    };

    render(
      <SessionProvider session={mockSession}>
        <RequireRole roles={['Admin']}>
          <div data-testid="admin-content">Admin Settings</div>
        </RequireRole>
        <RequireRole roles={['Instructor']}>
          <div data-testid="instructor-content">Instructor Dashboard</div>
        </RequireRole>
      </SessionProvider>
    );

    // Admin content should not be rendered
    expect(screen.queryByTestId('admin-content')).not.toBeInTheDocument();
    
    // Instructor content should be rendered
    expect(screen.getByTestId('instructor-content')).toBeInTheDocument();
  });
});
