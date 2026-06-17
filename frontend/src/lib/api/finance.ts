import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface FinanceRecord {
  id: string;
  [key: string]: any;
}

export const financeApi = {
  list: (cursor: string | null = null, signal?: AbortSignal) => 
    apiFetch<PaginatedResponse<FinanceRecord>>(`/finance?cursor=${cursor || ''}`, { signal }),
  listAll: () => fetchAllPages((cursor) => financeApi.list(cursor)),
  get: (id: string, signal?: AbortSignal) => 
    apiFetch<FinanceRecord>(`/finance/${id}`, { signal }),
  create: (data: Partial<FinanceRecord>, signal?: AbortSignal) => 
    apiFetch<FinanceRecord>(`/finance`, { method: 'POST', body: JSON.stringify(data), signal }),
  update: (id: string, data: Partial<FinanceRecord>, signal?: AbortSignal) => 
    apiFetch<FinanceRecord>(`/finance/${id}`, { method: 'PUT', body: JSON.stringify(data), signal }),
  delete: (id: string, signal?: AbortSignal) => 
    apiFetch<void>(`/finance/${id}`, { method: 'DELETE', signal }),
};
