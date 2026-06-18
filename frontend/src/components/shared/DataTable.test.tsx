import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { DataTable } from './DataTable';

describe('DataTable Component', () => {
  const columns = [
    { key: 'name', header: 'Name', accessor: 'name' as const },
    { key: 'age', header: 'Age', accessor: 'age' as const },
  ];

  it('DataTable_WithLoadingTrue_ShowsSkeletonRows', () => {
    const { container } = render(
      <DataTable columns={columns} data={[]} isLoading={true} />
    );

    // Skeleton elements should be rendered
    const skeletons = container.querySelectorAll('.animate-pulse'); // shadcn UI skeleton uses animate-pulse
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('DataTable_WithEmptyData_ShowsEmptyState', () => {
    render(<DataTable columns={columns} data={[]} emptyMessage="No users found" />);

    // Empty state should be visible
    expect(screen.getByText('No users found')).toBeInTheDocument();
    expect(screen.getByText('Não há dados disponíveis para exibir no momento.')).toBeInTheDocument();
  });
});
