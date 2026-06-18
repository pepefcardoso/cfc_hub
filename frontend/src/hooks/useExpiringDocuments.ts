import { useState, useEffect, useCallback } from 'react';
import { complianceApi, ExpiringDocument } from '@/lib/api/compliance';

export function useExpiringDocuments() {
  const [documents, setDocuments] = useState<ExpiringDocument[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchDocuments = useCallback(async (signal?: AbortSignal) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await complianceApi.getExpiringDocuments(signal);
      // Filter out any documents that are > 30 days away as an extra precaution
      // API should handle this, but it's safe to enforce it on the client
      const filteredData = data.filter(doc => doc.daysRemaining <= 30);
      setDocuments(filteredData);
    } catch (err: any) {
      if (err.name === 'AbortError') return;
      setError(err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    fetchDocuments(controller.signal);
    return () => controller.abort();
  }, [fetchDocuments]);

  return {
    documents,
    isLoading,
    error,
    refetch: () => fetchDocuments(),
  };
}
