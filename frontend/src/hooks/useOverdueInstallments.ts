import { useState, useEffect, useCallback } from 'react';
import { financeApi, Installment } from '@/lib/api/finance';
import { differenceInDays, parseISO } from 'date-fns';

export function useOverdueInstallments() {
  const [items, setItems] = useState<Installment[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<Error | null>(null);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState<boolean>(false);
  const [totalCount, setTotalCount] = useState<number>(0);

  const fetchOverdue = useCallback(async (cursor: string | null = null, signal?: AbortSignal) => {
    try {
      setLoading(true);
      setError(null);
      const result = await financeApi.getOverdueInstallments(cursor, signal);
      
      const enrichedItems = result.items.map(item => ({
        ...item,
        daysOverdue: Math.max(0, differenceInDays(new Date(), parseISO(item.dueDate)))
      }));

      setItems(prev => cursor ? [...prev, ...enrichedItems] : enrichedItems);
      setNextCursor(result.nextCursor);
      setHasMore(result.hasMore);
      setTotalCount(result.count);
    } catch (err: any) {
      if (err.name !== 'AbortError') {
        setError(err);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    fetchOverdue(null, controller.signal);
    return () => controller.abort();
  }, [fetchOverdue]);

  const loadMore = useCallback(() => {
    if (!loading && hasMore && nextCursor) {
      fetchOverdue(nextCursor);
    }
  }, [loading, hasMore, nextCursor, fetchOverdue]);

  return {
    items,
    loading,
    error,
    hasMore,
    totalCount,
    loadMore,
    mutate: () => fetchOverdue(null)
  };
}
