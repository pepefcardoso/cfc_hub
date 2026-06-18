import { useState, useEffect, useCallback } from 'react';
import { financeApi, PaymentPlan } from '@/lib/api/finance';

export function usePaymentPlan(studentId: string) {
  const [data, setData] = useState<PaymentPlan | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchPlan = useCallback(async (signal?: AbortSignal) => {
    try {
      setLoading(true);
      setError(null);
      const result = await financeApi.getPaymentPlan(studentId, signal);
      setData(result);
    } catch (err: any) {
      if (err.name !== 'AbortError') {
        setError(err);
      }
    } finally {
      setLoading(false);
    }
  }, [studentId]);

  useEffect(() => {
    const controller = new AbortController();
    if (studentId) {
      fetchPlan(controller.signal);
    }
    return () => controller.abort();
  }, [studentId, fetchPlan]);

  return {
    data,
    loading,
    error,
    mutate: fetchPlan
  };
}
