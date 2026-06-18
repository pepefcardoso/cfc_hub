import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { RequireRole } from './RequireRole';
import { SessionProvider } from '@/context/SessionContext';

describe('RequireRole Component', () => {
  it('RequireRole_AsReceptionist_DoesNotRenderAdminContent', () => {
    const mockSession = {
      role: 'Receptionist',
      tenantId: 'cfc_demo',
      userId: 'user-123',
      expiresAt: new Date(),
    };

    render(
      <SessionProvider session={mockSession}>
        <RequireRole roles={['Admin']}>
          <div data-testid="admin-content">Admin Settings</div>
        </RequireRole>
        <RequireRole roles={['Receptionist']}>
          <div data-testid="receptionist-content">Receptionist Dashboard</div>
        </RequireRole>
      </SessionProvider>
    );

    // Admin content should not be rendered
    expect(screen.queryByTestId('admin-content')).not.toBeInTheDocument();
    
    // Receptionist content should be rendered
    expect(screen.getByTestId('receptionist-content')).toBeInTheDocument();
  });
});
