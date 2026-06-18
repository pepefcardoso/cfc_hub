import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { StudentDetailCard } from './StudentDetailCard';

describe('StudentDetailCard Component', () => {
  it('StudentDetailCard_AsReceptionist_CpfIsRedacted', () => {
    // When a receptionist fetches student data, the backend redacts the CPF
    // by sending it as null. We simulate that here.
    const studentWithRedactedCpf = {
      id: 'student-1',
      name: 'John Doe',
      cpf: null, // Redacted
      rg: '123456789',
      dateOfBirth: '1990-01-01',
      email: 'john@example.com',
      phone: '11999999999',
      isActive: true,
      tenantId: 't1'
    };

    render(<StudentDetailCard student={studentWithRedactedCpf as any} />);

    // In StudentDetailCard, it renders: <DetailItem label="CPF" value={student.cpf} />
    // Since student.cpf is null, FieldRedacted should render its default lock icon
    // And there should be a label "CPF"
    expect(screen.getByText('CPF')).toBeInTheDocument();
    
    // We can't query by the exact label relation since it's a dl/dt/dd structure,
    // but we can query by the data-testid we added to FieldRedacted or the lock icon.
    // The component has 1 redacted field here (CPF).
    const redactedFields = screen.getAllByTestId('field-redacted');
    expect(redactedFields.length).toBe(1);
    
    const lockIcons = screen.getAllByTestId('lock-icon');
    expect(lockIcons.length).toBe(1);
  });
});
