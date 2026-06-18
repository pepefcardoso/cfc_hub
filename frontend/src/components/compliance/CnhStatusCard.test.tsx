import React from 'react';
import { render, screen } from '@testing-library/react';
import { CnhStatusCard } from './CnhStatusCard';
import { useCnhStatus } from '../../hooks/useCnhStatus';
import '@testing-library/jest-dom';

// Mock the hook
jest.mock('../../hooks/useCnhStatus');

describe('CnhStatusCard', () => {
  const mockUseCnhStatus = useCnhStatus as jest.Mock;

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('CnhStatusCard_WhenUnavailable_ShowsManualCheckMessage', () => {
    mockUseCnhStatus.mockReturnValue({
      data: { isAvailable: false },
      loading: false,
      error: null,
      refetch: jest.fn(),
    });

    render(<CnhStatusCard studentId="123" />);

    expect(
      screen.getByText('Consultar manualmente — sistema DETRAN indisponível no momento.')
    ).toBeInTheDocument();
  });

  it('shows countdown when rate limited with 429', () => {
    mockUseCnhStatus.mockReturnValue({
      data: null,
      loading: false,
      error: { status: 429, retryAfter: 30 },
      refetch: jest.fn(),
    });

    render(<CnhStatusCard studentId="123" />);

    expect(screen.getByText(/Aguarde 30s/i)).toBeInTheDocument();
  });
});
