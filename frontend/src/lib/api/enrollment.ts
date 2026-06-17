import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface Enrollment {
  id: string;
  [key: string]: any;
}

export const enrollmentApi = {
  list: (cursor: string | null = null, signal?: AbortSignal) => 
    apiFetch<PaginatedResponse<Enrollment>>(`/enrollments?cursor=${cursor || ''}`, { signal }),
  listAll: () => fetchAllPages((cursor) => enrollmentApi.list(cursor)),
  get: (id: string, signal?: AbortSignal) => 
    apiFetch<Enrollment>(`/enrollments/${id}`, { signal }),
  create: (data: Partial<Enrollment>, signal?: AbortSignal) => 
    apiFetch<Enrollment>(`/enrollments`, { method: 'POST', body: JSON.stringify(data), signal }),
  update: (id: string, data: Partial<Enrollment>, signal?: AbortSignal) => 
    apiFetch<Enrollment>(`/enrollments/${id}`, { method: 'PUT', body: JSON.stringify(data), signal }),
  delete: (id: string, signal?: AbortSignal) => 
    apiFetch<void>(`/enrollments/${id}`, { method: 'DELETE', signal }),
};
