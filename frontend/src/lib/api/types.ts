export interface PaginatedResponse<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
  count: number;
}

export interface ApiError {
  status: number;
  type: string;
  detail: string;
  errors?: Record<string, string[]>;
  traceId: string;
}

export interface Session {
  accessToken: string;
  role: string;
  tenantId: string;
  expiresAt: string;
}
