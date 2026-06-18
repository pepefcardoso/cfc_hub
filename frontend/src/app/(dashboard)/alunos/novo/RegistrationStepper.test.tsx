import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { RegistrationStepper } from './RegistrationStepper';

// Mock the CPF validation so we don't have to provide a real valid CPF in tests
vi.mock('@/lib/cpf', () => ({
  isValidCpf: vi.fn(() => true),
  formatCpf: vi.fn((val) => val),
}));

// Mock hash function to avoid crypto.subtle in jsdom
vi.mock('@/lib/hash', () => ({
  generateSHA256: vi.fn(async () => 'mocked-hash'),
}));

// Mock the router
vi.mock('next/navigation', () => ({
  useRouter: () => ({
    push: vi.fn(),
  }),
}));

describe('RegistrationStepper', () => {
  it('StepConsent_WithoutCheckbox_DisablesSubmitButton', async () => {
    render(<RegistrationStepper />);

    // Step 1: Personal Data
    fireEvent.change(screen.getByLabelText(/Nome Completo/i), { target: { value: 'João Silva' } });
    fireEvent.change(screen.getByLabelText(/CPF/i), { target: { value: '12345678909' } });
    fireEvent.change(screen.getByLabelText(/E-mail/i), { target: { value: 'joao@exemplo.com' } });
    fireEvent.change(screen.getByLabelText(/Telefone/i), { target: { value: '+5511999999999' } });
    
    // For date, 20 years ago
    const dob = new Date();
    dob.setFullYear(dob.getFullYear() - 20);
    fireEvent.change(screen.getByLabelText(/Data de Nascimento/i), { target: { value: dob.toISOString().split('T')[0] } });

    // Click next
    fireEvent.click(screen.getByRole('button', { name: /Próximo/i }));

    // Step 2: Address
    await waitFor(() => {
      expect(screen.getByLabelText(/Rua \/ Logradouro/i)).toBeInTheDocument();
    });

    fireEvent.change(screen.getAllByLabelText(/CEP/i)[0], { target: { value: '01000-000' } });
    fireEvent.change(screen.getAllByLabelText(/Rua \/ Logradouro/i)[0], { target: { value: 'Rua Direita' } });
    fireEvent.change(screen.getAllByLabelText(/Número/i)[0], { target: { value: '10' } });
    fireEvent.change(screen.getAllByLabelText(/Bairro/i)[0], { target: { value: 'Sé' } });
    fireEvent.change(screen.getAllByLabelText(/Cidade/i)[0], { target: { value: 'São Paulo' } });
    
    // Select state (SP)
    // Note: Radix UI Select requires interacting with trigger and then item, but we can bypass the complex Radix Select interaction in simple tests by mocking or using Form components correctly, or using a fallback. For simplicity, since it's a hidden native input in some setups or we can just bypass it by setting the form value directly. But react-hook-form needs interaction.
    // Let's use the hook form's ability to trigger by finding the role="combobox"
    const user = userEvent.setup();
    const combobox = screen.getByRole('combobox');
    await user.click(combobox);
    
    await waitFor(async () => {
      const spOption = screen.getByText('SP');
      await user.click(spOption);
    });

    // Click next
    fireEvent.click(screen.getByRole('button', { name: /Próximo/i }));

    // Step 3: Consent
    await waitFor(() => {
      expect(screen.getByText(/Li e aceito a Política de Privacidade/i)).toBeInTheDocument();
    });

    const submitBtn = screen.getByRole('button', { name: /Finalizar Cadastro/i });

    // Verify it is disabled initially
    expect(submitBtn).toBeDisabled();

    // Click checkbox
    const checkbox = screen.getByRole('checkbox');
    fireEvent.click(checkbox);

    // Verify it is enabled after checking
    await waitFor(() => {
      expect(submitBtn).not.toBeDisabled();
    });
  });
});
