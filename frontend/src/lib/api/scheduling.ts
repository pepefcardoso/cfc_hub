import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface ScheduleItem {
  id: string;
  [key: string]: any;
}

export const schedulingApi = {
  list: (cursor: string | null = null, signal?: AbortSignal) => 
    apiFetch<PaginatedResponse<ScheduleItem>>(`/scheduling?cursor=${cursor || ''}`, { signal }),
  listAll: () => fetchAllPages((cursor) => schedulingApi.list(cursor)),
  get: (id: string, signal?: AbortSignal) => 
    apiFetch<ScheduleItem>(`/scheduling/${id}`, { signal }),
  create: (data: Partial<ScheduleItem>, signal?: AbortSignal) => 
    apiFetch<ScheduleItem>(`/scheduling`, { method: 'POST', body: JSON.stringify(data), signal }),
  update: (id: string, data: Partial<ScheduleItem>, signal?: AbortSignal) => 
    apiFetch<ScheduleItem>(`/scheduling/${id}`, { method: 'PUT', body: JSON.stringify(data), signal }),
  delete: (id: string, signal?: AbortSignal) => 
    apiFetch<void>(`/scheduling/${id}`, { method: 'DELETE', signal }),
};
