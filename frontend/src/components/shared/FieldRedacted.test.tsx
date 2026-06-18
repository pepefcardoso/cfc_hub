import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { FieldRedacted } from './FieldRedacted';

describe('FieldRedacted Component', () => {
  it('FieldRedacted_WhenValueIsNull_RendersLockIcon', () => {
    render(<FieldRedacted value={null} />);

    expect(screen.getByTestId('lock-icon')).toBeInTheDocument();
    expect(screen.getByTestId('field-redacted')).toBeInTheDocument();
  });

  it('FieldRedacted_WhenValueIsPresent_RendersValue', () => {
    render(<FieldRedacted value="Confidential Data" />);

    expect(screen.getByText('Confidential Data')).toBeInTheDocument();
    expect(screen.queryByTestId('lock-icon')).not.toBeInTheDocument();
  });
});
