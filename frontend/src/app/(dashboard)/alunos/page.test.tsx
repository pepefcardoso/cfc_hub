import { vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import StudentListPage from './page';
import { useStudents } from '@/hooks/useStudent';
import { useRouter, useSearchParams, usePathname } from 'next/navigation';

vi.mock('@/hooks/useStudent');
vi.mock('next/navigation', () => ({
  useRouter: vi.fn(),
  useSearchParams: vi.fn(),
  usePathname: vi.fn(),
}));

describe('StudentListPage', () => {
  beforeEach(() => {
    (useRouter as vi.Mock).mockReturnValue({ replace: vi.fn() });
    (usePathname as vi.Mock).mockReturnValue('/alunos');
    (useSearchParams as vi.Mock).mockReturnValue(new URLSearchParams());
  });

  it('CPF column renders FieldRedacted for roles without access (cpf is null)', () => {
    (useStudents as vi.Mock).mockReturnValue({
      students: [
        { id: '1', name: 'João', cpf: null, status: 'Active' },
        { id: '2', name: 'Maria', cpf: '123.456.789-00', status: 'Active' },
      ],
      isLoading: false,
      hasMore: false,
      nextCursor: null,
      loadMore: vi.fn(),
    });

    render(<StudentListPage />);

    // Maria's CPF should be visible
    expect(screen.getByText('123.456.789-00')).toBeInTheDocument();

    // João's CPF should be redacted (FieldRedacted renders an aria-label "Campo restrito" or a Lock icon)
    // Looking for the aria-label from FieldRedacted
    const redactedFields = screen.getAllByLabelText('Campo restrito');
    expect(redactedFields.length).toBeGreaterThan(0);
  });
});
