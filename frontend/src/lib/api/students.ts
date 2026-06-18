import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface Student {
  id: string;
  name: string;
  cpf: string;
}

export const studentsApi = {
  list: (q: string = '', cursor: string | null = null, signal?: AbortSignal) => {
    const params = new URLSearchParams();
    if (q) params.append('q', q);
    if (cursor) params.append('cursor', cursor);
    return apiFetch<PaginatedResponse<Student>>(`/students?${params.toString()}`, { signal });
  },
  listAll: (q: string = '') => fetchAllPages((cursor) => studentsApi.list(q, cursor)),
  get: (id: string, signal?: AbortSignal) => 
    apiFetch<Student>(`/students/${id}`, { signal }),
};
