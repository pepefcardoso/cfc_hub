import { useState, useEffect, useCallback } from 'react';
import { studentsApi, CnhStatus } from '../lib/api/students';
import { ApiError } from '../lib/api/errors';

interface CacheEntry {
  data: CnhStatus;
  timestamp: number;
}

const cnhCache: Record<string, CacheEntry> = {};
const CACHE_TTL = 24 * 60 * 60 * 1000; // 24 hours

export function useCnhStatus(studentId: string) {
  const [data, setData] = useState<CnhStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);

  const fetchStatus = useCallback(async (force = false) => {
    if (!studentId) return;

    if (!force) {
      const cached = cnhCache[studentId];
      if (cached && Date.now() - cached.timestamp < CACHE_TTL) {
        setData(cached.data);
        setLoading(false);
        setError(null);
        return;
      }
    }

    setLoading(true);
    setError(null);

    try {
      const result = await studentsApi.getCnhStatus(studentId);
      const dataWithTimestamp = {
        ...result,
        fetchedAt: result.fetchedAt || new Date().toISOString()
      };
      cnhCache[studentId] = {
        data: dataWithTimestamp,
        timestamp: Date.now()
      };
      setData(dataWithTimestamp);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err);
      } else {
        setError(new ApiError({ status: 500, type: 'Unknown', detail: 'Erro desconhecido', traceId: '' }));
      }
    } finally {
      setLoading(false);
    }
  }, [studentId]);

  useEffect(() => {
    fetchStatus();
  }, [fetchStatus]);

  return {
    data,
    loading,
    error,
    refetch: () => fetchStatus(true)
  };
}
