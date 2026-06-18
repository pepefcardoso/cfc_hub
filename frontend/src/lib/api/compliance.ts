import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface ComplianceRecord {
  id: string;
  [key: string]: any;
}

export type AlertTier = 'D1' | 'D7' | 'D15' | 'D30';

export interface ExpiringDocument {
  id: string;
  studentId: string;
  studentName: string;
  documentType: string;
  expiryDate: string;
  daysRemaining: number;
  alertTier: AlertTier;
}

export interface PresignedUrlResponse {
  id: string;
  uploadUrl: string;
  expiresAt: string;
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
  
  // Document Tracking and Expiry
  getExpiringDocuments: (signal?: AbortSignal) =>
    apiFetch<ExpiringDocument[]>('/compliance/documents/expiring', { signal }),
  
  requestDocumentUpload: (data: { studentId: string; documentType: string; fileName: string; contentType: string; }, signal?: AbortSignal) =>
    apiFetch<PresignedUrlResponse>('/compliance/documents', {
      method: 'POST',
      body: JSON.stringify(data),
      signal,
    }),
  
  confirmDocumentUpload: (id: string, data: { s3Key: string; }, signal?: AbortSignal) =>
    apiFetch<void>(`/compliance/documents/${id}/confirm`, {
      method: 'PATCH',
      body: JSON.stringify(data),
      signal,
    }),
};
