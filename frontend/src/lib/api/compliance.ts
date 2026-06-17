import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface ComplianceRecord {
  id: string;
  [key: string]: any;
}

export const complianceApi = {
  list: (cursor: string | null = null, signal?: AbortSignal) => 
    apiFetch<PaginatedResponse<ComplianceRecord>>(`/compliance?cursor=${cursor || ''}`, { signal }),
  listAll: () => fetchAllPages((cursor) => complianceApi.list(cursor)),
  get: (id: string, signal?: AbortSignal) => 
    apiFetch<ComplianceRecord>(`/compliance/${id}`, { signal }),
  create: (data: Partial<ComplianceRecord>, signal?: AbortSignal) => 
    apiFetch<ComplianceRecord>(`/compliance`, { method: 'POST', body: JSON.stringify(data), signal }),
  update: (id: string, data: Partial<ComplianceRecord>, signal?: AbortSignal) => 
    apiFetch<ComplianceRecord>(`/compliance/${id}`, { method: 'PUT', body: JSON.stringify(data), signal }),
  delete: (id: string, signal?: AbortSignal) => 
    apiFetch<void>(`/compliance/${id}`, { method: 'DELETE', signal }),
};
