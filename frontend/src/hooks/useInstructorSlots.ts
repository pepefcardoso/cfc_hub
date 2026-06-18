import useSWRInfinite from 'swr/infinite';
import { schedulingApi, Slot } from '@/lib/api/scheduling';
import type { PaginatedResponse } from '@/lib/api/types';

export function useInstructorSlots(instructorId: string) {
  const getKey = (pageIndex: number, previousPageData: PaginatedResponse<Slot> | null) => {
    if (previousPageData && !previousPageData.hasMore) return null; // reached the end
    if (!instructorId) return null;
    return ['instructorSlots', instructorId, previousPageData?.nextCursor ?? null];
  };

  const fetcher = ([, id, cursor]: [string, string, string | null]) => {
    return schedulingApi.getInstructorSlots(id, cursor);
  };

  const { data, error, size, setSize, mutate, isLoading, isValidating } = useSWRInfinite(
    getKey,
    fetcher
  );

  const updateSlotStatus = async (slotId: string, status: string, reason?: string) => {
    const originalData = data;
    
    // Optimistic update
    const optimisticData = data?.map(page => ({
      ...page,
      items: page.items.map(slot => 
        slot.id === slotId ? { ...slot, status: status as Slot['status'] } : slot
      )
    }));

    mutate(optimisticData, false);

    try {
      await schedulingApi.updateSlotStatus(slotId, status, reason);
      mutate(); // Revalidate
    } catch (err) {
      mutate(originalData, false); // Revert
      throw err;
    }
  };

  return {
    data,
    slots: data ? data.flatMap(page => page.items) : [],
    isLoading,
    isValidating,
    error,
    size,
    setSize,
    hasMore: data?.[data.length - 1]?.hasMore ?? false,
    updateSlotStatus
  };
}
