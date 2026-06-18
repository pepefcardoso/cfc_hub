import React from 'react';
import { render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import { PaymentPlanTable } from './PaymentPlanTable';
import { PaymentPlan, Installment } from '@/lib/api/finance';

// Mock SessionContext to simulate different roles
let mockRole = 'Admin';
jest.mock('@/context/SessionContext', () => ({
  useSession: () => ({ role: mockRole })
}));

describe('PaymentPlanTable', () => {
  const mockInstallment: Installment = {
    id: '1',
    studentId: 's1',
    number: 1,
    amount: 500,
    dueDate: '2025-01-01',
    status: 'Pending',
    amountPaid: 0
  };

  const mockPaymentPlan: PaymentPlan = {
    studentId: 's1',
    installments: [mockInstallment],
    totalAmount: 500,
    totalPaid: 0
  };

  const mockOnRecordPayment = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders correctly with correct currency formatting', () => {
    mockRole = 'Admin';
    render(<PaymentPlanTable paymentPlan={mockPaymentPlan} onRecordPayment={mockOnRecordPayment} />);
    
    // Check if the amount is formatted as BRL
    expect(screen.getByText('R$ 500,00')).toBeInTheDocument();
    
    // Check if the "Registrar pagamento" action is visible for Admin
    expect(screen.getByRole('button', { name: /Registrar pagamento/i })).toBeInTheDocument();
  });

  it('PaymentPlanTable_AsInstructor_DoesNotRenderPaymentAction', () => {
    mockRole = 'Instructor';
    render(<PaymentPlanTable paymentPlan={mockPaymentPlan} onRecordPayment={mockOnRecordPayment} />);
    
    // Check if the amount is formatted as BRL
    expect(screen.getByText('R$ 500,00')).toBeInTheDocument();
    
    // Instructor should NOT see the action button
    expect(screen.queryByRole('button', { name: /Registrar pagamento/i })).not.toBeInTheDocument();
  });

  it('PaymentPlanTable_AsReceptionist_DoesNotRenderPaymentAction', () => {
    mockRole = 'Receptionist';
    render(<PaymentPlanTable paymentPlan={mockPaymentPlan} onRecordPayment={mockOnRecordPayment} />);
    
    // Receptionist should NOT see the action button
    expect(screen.queryByRole('button', { name: /Registrar pagamento/i })).not.toBeInTheDocument();
  });
  
  it('PaymentPlanTable_AsFinancial_RendersPaymentAction', () => {
    mockRole = 'Financial';
    render(<PaymentPlanTable paymentPlan={mockPaymentPlan} onRecordPayment={mockOnRecordPayment} />);
    
    // Financial should see the action button
    expect(screen.getByRole('button', { name: /Registrar pagamento/i })).toBeInTheDocument();
  });
});
