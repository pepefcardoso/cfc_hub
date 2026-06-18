import { vi } from 'vitest';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { SignatureCapture } from './SignatureCapture';

// Mock react-signature-canvas since it requires a real canvas environment
vi.mock('react-signature-canvas', () => {
  return {
    default: React.forwardRef((props: any, ref: any) => {
      React.useImperativeHandle(ref, () => ({
        isEmpty: () => props['data-empty'] !== false,
        clear: () => {},
        getTrimmedCanvas: () => ({
          toDataURL: () => 'data:image/png;base64,mockedbase64'
        })
      }));

      return (
        <div 
          data-testid="mock-signature-canvas" 
          onClick={props.onEnd}
        />
      );
    })
  };
});

describe('SignatureCapture', () => {
  it('SignatureCapture_WhenEmpty_DisablesSubmitButton', () => {
    const mockOnSign = vi.fn();
    
    render(
      <SignatureCapture 
        onSign={mockOnSign} 
        isSigning={false} 
        errorState={null} 
      />
    );

    const submitButton = screen.getByRole('button', { name: /Assinar Contrato/i });
    
    // Initial state: canvas is empty, button should be disabled
    expect(submitButton).toBeDisabled();

    // Simulate drawing on canvas (which triggers onEnd)
    const canvas = screen.getByTestId('mock-signature-canvas');
    fireEvent.click(canvas); // click triggers onEnd in our mock

    // After drawing, button should be enabled
    expect(submitButton).not.toBeDisabled();
  });
});
