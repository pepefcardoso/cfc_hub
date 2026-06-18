import { apiFetch, fetchAllPages } from './client';
import type { PaginatedResponse } from './types';

export interface FinanceRecord {
  id: string;
  [key: string]: any;
}

export type InstallmentStatus = 'Pending' | 'Paid' | 'Overdue' | 'Cancelled';

export interface Installment {
  id: string;
  studentId: string;
  studentName?: string;
  number: number;
  amount: number;
  dueDate: string;
  status: InstallmentStatus;
  amountPaid: number;
  daysOverdue?: number;
}

export interface PaymentPlan {
  studentId: string;
  installments: Installment[];
  totalAmount: number;
  totalPaid: number;
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

  // Payment plan specific endpoints
  getPaymentPlan: (studentId: string, signal?: AbortSignal) => 
    apiFetch<PaymentPlan>(`/finance/students/${studentId}/payment-plan`, { signal }),
  getOverdueInstallments: (cursor: string | null = null, signal?: AbortSignal) =>
    apiFetch<PaginatedResponse<Installment>>(`/finance/installments/overdue?cursor=${cursor || ''}`, { signal }),
  recordPayment: (data: { installmentId: string; method: string; amount: number }, signal?: AbortSignal) =>
    apiFetch<void>(`/payments`, { method: 'POST', body: JSON.stringify(data), signal }),
};

