import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import userEvent from '@testing-library/user-event';
import { useForm, FormProvider } from 'react-hook-form';
import { StepConsent } from './StepConsent';
import { Button } from '@/components/ui/button';

// Wrapper to provide react-hook-form context
function FormWrapper({ children }: { children: React.ReactNode }) {
  const methods = useForm({
    defaultValues: {
      consentimento: false,
    },
  });

  return (
    <FormProvider {...methods}>
      <form onSubmit={methods.handleSubmit(() => {})}>
        {children}
        <Button type="submit" disabled={!methods.watch('consentimento')}>Submit</Button>
      </form>
    </FormProvider>
  );
}

describe('StepConsent Component', () => {
  it('StepConsent_WithoutCheckbox_DisablesSubmitButton', async () => {
    const user = userEvent.setup();
    render(
      <FormWrapper>
        <StepConsent />
      </FormWrapper>
    );

    const submitButton = screen.getByRole('button', { name: /Submit/i });
    const checkbox = screen.getByRole('checkbox');

    // Initially checkbox is not checked, submit button should be disabled
    expect(checkbox).not.toBeChecked();
    expect(submitButton).toBeDisabled();

    // Check it
    await user.click(checkbox);
    expect(checkbox).toBeChecked();
    
    // Using waitFor in case react-hook-form needs a tick to update the watched value
    await waitFor(() => {
      expect(submitButton).toBeEnabled();
    });

    // Uncheck it
    await user.click(checkbox);
    await waitFor(() => {
      expect(submitButton).toBeDisabled();
    });
  });
});
