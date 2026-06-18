import { apiFetch } from './client';

export interface CfcPublicProfile {
  name: string;
  tradingName: string;
  cnpj: string;
  address: {
    street: string;
    number: string;
    complement?: string;
    neighborhood: string;
    city: string;
    state: string;
    zipCode: string;
  };
  contact: {
    phone: string;
    email: string;
  };
  categories: string[];
}

export interface QrCodeValidationResult {
  documentType: string;
  studentName: string;
  issueDate: string;
  validUntil?: string;
  isValid: boolean;
}

export async function getCfcProfile(slug: string): Promise<CfcPublicProfile> {
  return apiFetch<CfcPublicProfile>(`/public/cfc/${slug}`);
}

export async function validateQrCode(code: string): Promise<QrCodeValidationResult> {
  return apiFetch<QrCodeValidationResult>(`/public/qr/${code}`);
}
