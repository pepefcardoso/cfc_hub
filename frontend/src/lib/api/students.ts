import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface Student {
  id: string;
  name: string;
  cpf: string | null;
  rg?: string | null;
  phone?: string | null;
  email?: string | null;
  dateOfBirth?: string | null;
  status?: string;
  isActive?: boolean;
}

export interface CnhStatus {
  isAvailable: boolean;
  status?: string;
  expiryDate?: string;
  points?: number;
  fetchedAt?: string;
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
  delete: (id: string, signal?: AbortSignal) =>
    apiFetch<void>(`/students/${id}`, { method: 'DELETE', signal }),
  createEnrollment: (studentId: string, category: string, signal?: AbortSignal) =>
    apiFetch<any>(`/students/${studentId}/enrollments`, { 
      method: 'POST', 
      body: JSON.stringify({ category }), 
      signal 
    }),
  getEnrollments: (studentId: string, signal?: AbortSignal) =>
    apiFetch<any[]>(`/students/${studentId}/enrollments`, { signal }),
  getCnhStatus: (studentId: string, signal?: AbortSignal) =>
    apiFetch<CnhStatus>(`/students/${studentId}/cnh-status`, { signal }),
};
