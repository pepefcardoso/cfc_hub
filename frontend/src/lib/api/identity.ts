import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface IdentityUser {
  id: string;
  [key: string]: any;
}

export const identityApi = {
  list: (cursor: string | null = null, signal?: AbortSignal) => 
    apiFetch<PaginatedResponse<IdentityUser>>(`/identity/users?cursor=${cursor || ''}`, { signal }),
  listAll: () => fetchAllPages((cursor) => identityApi.list(cursor)),
  get: (id: string, signal?: AbortSignal) => 
    apiFetch<IdentityUser>(`/identity/users/${id}`, { signal }),
  create: (data: Partial<IdentityUser>, signal?: AbortSignal) => 
    apiFetch<IdentityUser>(`/identity/users`, { method: 'POST', body: JSON.stringify(data), signal }),
  update: (id: string, data: Partial<IdentityUser>, signal?: AbortSignal) => 
    apiFetch<IdentityUser>(`/identity/users/${id}`, { method: 'PUT', body: JSON.stringify(data), signal }),
  delete: (id: string, signal?: AbortSignal) => 
    apiFetch<void>(`/identity/users/${id}`, { method: 'DELETE', signal }),
};
