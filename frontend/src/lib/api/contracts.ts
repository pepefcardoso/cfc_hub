import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface Contract {
  id: string;
  [key: string]: any;
}

export const contractsApi = {
  list: (cursor: string | null = null, signal?: AbortSignal) => 
    apiFetch<PaginatedResponse<Contract>>(`/contracts?cursor=${cursor || ''}`, { signal }),
  listAll: () => fetchAllPages((cursor) => contractsApi.list(cursor)),
  get: (id: string, signal?: AbortSignal) => 
    apiFetch<Contract>(`/contracts/${id}`, { signal }),
  create: (data: Partial<Contract>, signal?: AbortSignal) => 
    apiFetch<Contract>(`/contracts`, { method: 'POST', body: JSON.stringify(data), signal }),
  update: (id: string, data: Partial<Contract>, signal?: AbortSignal) => 
    apiFetch<Contract>(`/contracts/${id}`, { method: 'PUT', body: JSON.stringify(data), signal }),
  delete: (id: string, signal?: AbortSignal) => 
    apiFetch<void>(`/contracts/${id}`, { method: 'DELETE', signal }),
};
