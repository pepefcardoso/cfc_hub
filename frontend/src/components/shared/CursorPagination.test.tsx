import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import userEvent from '@testing-library/user-event';
import { CursorPagination } from './CursorPagination';

describe('CursorPagination Component', () => {
  it('CursorPagination_WhenHasMoreFalse_DoesNotRenderButton', () => {
    const onNext = vi.fn();
    const { container } = render(
      <CursorPagination hasMore={false} onNext={onNext} />
    );

    expect(screen.queryByRole('button', { name: /carregar mais/i })).not.toBeInTheDocument();
    expect(container.firstChild).toBeNull();
  });

  it('Renders button when hasMore is true and triggers onNext on click', async () => {
    const user = userEvent.setup();
    const onNext = vi.fn();
    render(
      <CursorPagination hasMore={true} nextCursor="cursor-123" onNext={onNext} />
    );

    const button = screen.getByRole('button', { name: /carregar mais/i });
    expect(button).toBeInTheDocument();

    await user.click(button);
    expect(onNext).toHaveBeenCalledWith('cursor-123');
  });
});
