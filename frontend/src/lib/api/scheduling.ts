import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface ScheduleItem {
  id: string;
  [key: string]: any;
}

export interface Slot {
  id: string;
  date: string;
  startTime: string;
  endTime: string;
  instructorId?: string;
  instructorName?: string;
  vehicleId?: string;
  trackType?: string;
  status?: 'Available' | 'Booked' | 'Confirmed' | 'Cancelled' | 'Completed' | 'NoShow';
}

export interface BookSlotRequest {
  studentId: string;
  date: string;
  startTime: string;
  category: string;
  instructorId?: string;
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
    
  getAvailableSlots: (date: string, category: string, instructorId?: string, signal?: AbortSignal) => {
    const params = new URLSearchParams({ date, category });
    if (instructorId) params.append('instructorId', instructorId);
    return apiFetch<Slot[]>(`/scheduling/slots/available?${params.toString()}`, { signal });
  },
  getBookedSlots: (date: string, category: string, instructorId?: string, signal?: AbortSignal) => {
    const params = new URLSearchParams({ date, category });
    if (instructorId) params.append('instructorId', instructorId);
    return apiFetch<Slot[]>(`/scheduling/slots/booked?${params.toString()}`, { signal });
  },
  bookSlot: (data: BookSlotRequest, signal?: AbortSignal) =>
    apiFetch<Slot>(`/scheduling/slots`, { method: 'POST', body: JSON.stringify(data), signal }),
  getStudentSlots: (studentId: string, cursor: string | null = null, signal?: AbortSignal) =>
    apiFetch<PaginatedResponse<Slot>>(`/scheduling/slots/student/${studentId}?cursor=${cursor || ''}`, { signal }),
  getInstructorSlots: (instructorId: string, cursor: string | null = null, signal?: AbortSignal) =>
    apiFetch<PaginatedResponse<Slot>>(`/scheduling/slots/instructor/${instructorId}?cursor=${cursor || ''}`, { signal }),
  updateSlotStatus: (id: string, status: string, reason?: string, signal?: AbortSignal) =>
    apiFetch<Slot>(`/scheduling/slots/${id}/status`, { 
      method: 'PATCH', 
      body: JSON.stringify({ status, reason }), 
      signal 
    }),
};
