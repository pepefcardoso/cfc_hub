import { ApiError, NetworkError } from './errors';
import { getSession, clearSessionCache } from './auth';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api/v1';

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const session = await getSession();
  
  const headers = new Headers(options.headers);
  if (session?.accessToken) {
    headers.set('Authorization', `Bearer ${session.accessToken}`);
  }
  if (!headers.has('Content-Type') && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...options,
      headers,
    });
  } catch (error) {
    throw new NetworkError(error instanceof Error ? error.message : 'Network failure');
  }

  if (response.status === 401) {
    clearSessionCache();
    if (typeof window !== 'undefined') {
      window.location.href = '/login';
    }
  }

  if (!response.ok) {
    let errorData;
    try {
      errorData = await response.json();
    } catch {
      throw new ApiError({
        status: response.status,
        type: 'about:blank',
        detail: response.statusText,
        traceId: '',
        retryAfter: response.status === 429 && response.headers.has('Retry-After') 
          ? parseInt(response.headers.get('Retry-After') || '0', 10) 
          : undefined
      });
    }
    throw new ApiError({
      status: errorData.status || response.status,
      type: errorData.type || 'about:blank',
      detail: errorData.detail || response.statusText,
      errors: errorData.errors,
      traceId: errorData.traceId || '',
      retryAfter: response.status === 429 && response.headers.has('Retry-After') 
        ? parseInt(response.headers.get('Retry-After') || '0', 10) 
        : undefined
    });
  }

  if (response.status === 204) {
    return {} as T;
  }

  const data = await response.json();
  if (data && typeof data === 'object' && 'data' in data) {
    return data.data as T;
  }
  return data as T;
}

export async function fetchAllPages<T>(
  fetcher: (cursor: string | null) => Promise<{ items: T[]; nextCursor: string | null; hasMore: boolean; count: number }>,
): Promise<T[]> {
  const allItems: T[] = [];
  let cursor: string | null = null;
  let hasMore = true;

  while (hasMore) {
    const page = await fetcher(cursor);
    allItems.push(...page.items);
    cursor = page.nextCursor;
    hasMore = page.hasMore;
  }

  return allItems;
}
