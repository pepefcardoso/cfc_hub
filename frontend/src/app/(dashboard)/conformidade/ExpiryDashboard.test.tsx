import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { ExpiryDashboard } from './ExpiryDashboard';
import { useExpiringDocuments } from '@/hooks/useExpiringDocuments';

// Mock the custom hook
jest.mock('@/hooks/useExpiringDocuments');

const mockDocuments = [
  {
    id: 'doc-1',
    studentId: 's-1',
    studentName: 'João Silva',
    documentType: 'MedicalExam',
    expiryDate: new Date(Date.now() + 86400000).toISOString(), // 1 day from now
    daysRemaining: 1,
    alertTier: 'D1'
  },
  {
    id: 'doc-2',
    studentId: 's-2',
    studentName: 'Maria Santos',
    documentType: 'PsychologicalExam',
    expiryDate: new Date(Date.now() + 86400000 * 5).toISOString(), // 5 days from now
    daysRemaining: 5,
    alertTier: 'D7'
  },
  {
    id: 'doc-3',
    studentId: 's-3',
    studentName: 'Pedro Costa',
    documentType: 'MedicalExam',
    expiryDate: new Date(Date.now() + 86400000 * 25).toISOString(), // 25 days from now
    daysRemaining: 25,
    alertTier: 'D30'
  }
];

describe('ExpiryDashboard', () => {
  it('ExpiryDashboard_GroupsAlertsByTier', async () => {
    // Setup mock to return predefined documents
    (useExpiringDocuments as jest.Mock).mockReturnValue({
      documents: mockDocuments,
      isLoading: false,
      error: null,
      refetch: jest.fn()
    });

    render(<ExpiryDashboard />);

    // Wait for the components to render
    await waitFor(() => {
      // Check for tier section titles (they should exist based on mock data)
      expect(screen.getByText('Crítico (1 dia)')).toBeInTheDocument();
      expect(screen.getByText('Atenção (7 dias)')).toBeInTheDocument();
      expect(screen.getByText('Alerta (30 dias)')).toBeInTheDocument();
      
      // Notice D15 shouldn't be rendered if there are no items in that tier
      expect(screen.queryByText('Aviso (15 dias)')).not.toBeInTheDocument();
      
      // Check that student names are rendered (which means the alerts are in the right groups and rendered)
      // D1 is expanded by default, D7 is expanded by default in our state
      expect(screen.getByText('João Silva')).toBeInTheDocument();
      expect(screen.getByText('Maria Santos')).toBeInTheDocument();
      
      // D30 is collapsed by default, but it's not removed from DOM in standard hidden implementations,
      // Wait, in our implementation it uses `isExpanded && <div...>` so it IS removed from DOM if collapsed.
      expect(screen.queryByText('Pedro Costa')).not.toBeInTheDocument();
    });
  });
});
