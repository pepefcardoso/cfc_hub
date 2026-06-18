import type { ApiError as IApiError } from './types';

export class ApiError extends Error implements IApiError {
  status: number;
  type: string;
  detail: string;
  errors?: Record<string, string[]>;
  traceId: string;
  retryAfter?: number;

  constructor(data: IApiError) {
    super(data.detail || 'API Error');
    this.name = 'ApiError';
    this.status = data.status;
    this.type = data.type;
    this.detail = data.detail;
    this.errors = data.errors;
    this.traceId = data.traceId;
    this.retryAfter = data.retryAfter;
  }
}

export class NetworkError extends Error {
  constructor(message: string = 'Network failure') {
    super(message);
    this.name = 'NetworkError';
  }
}
