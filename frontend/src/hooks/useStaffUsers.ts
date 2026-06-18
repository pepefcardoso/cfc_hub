import useSWRInfinite from 'swr/infinite';
import { apiFetch } from '@/lib/api/client';
import { RoleType } from '@/lib/permissions';

export interface StaffUser {
  id: string;
  name: string;
  email: string;
  role: RoleType;
  isActive: boolean;
  lastAccessAt: string | null;
}

interface StaffUsersResponse {
  items: StaffUser[];
  nextCursor: string | null;
  hasMore: boolean;
  count: number;
}

export function useStaffUsers(limit = 10) {
  const getKey = (pageIndex: number, previousPageData: StaffUsersResponse | null) => {
    // Reached the end
    if (previousPageData && !previousPageData.hasMore) return null;

    // First page, we don't have previousPageData
    if (pageIndex === 0) return `/staff?limit=${limit}`;

    // Add the cursor to the API endpoint
    return `/staff?limit=${limit}&cursor=${previousPageData?.nextCursor}`;
  };

  const { data, error, size, setSize, isLoading, mutate } = useSWRInfinite<StaffUsersResponse>(
    getKey,
    (url: string) => apiFetch<StaffUsersResponse>(url)
  );

  const users = data ? data.flatMap(page => page.items) : [];
  const isLoadingMore = isLoading || (size > 0 && data && typeof data[size - 1] === 'undefined');
  const isEmpty = data?.[0]?.items.length === 0;
  const hasMore = data?.[data.length - 1]?.hasMore || false;
  const nextCursor = data?.[data.length - 1]?.nextCursor || null;

  const loadMore = () => {
    setSize(size + 1);
  };

  return {
    users,
    hasMore,
    nextCursor,
    isLoading: isLoadingMore || isLoading,
    isEmpty,
    loadMore,
    error,
    mutate,
  };
}

export async function inviteStaffUser(data: { name: string; email: string; role: string; password?: string }) {
  return apiFetch('/staff', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function changeStaffRole(userId: string, newRole: RoleType) {
  return apiFetch(`/staff/${userId}/role`, {
    method: 'PATCH',
    body: JSON.stringify({ role: newRole }),
  });
}

export async function deactivateStaffUser(userId: string) {
  return apiFetch(`/staff/${userId}/deactivate`, {
    method: 'POST',
  });
}
