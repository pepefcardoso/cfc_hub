import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '@/tests/mocks/server';
import { apiFetch } from './client';
import * as auth from './auth';

describe('apiFetch Client', () => {
  beforeEach(() => {
    auth.clearSessionCache();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('apiFetch_On401_ClearsSessionAndRedirects', async () => {
    // Setup window object to mock window.location.href
    const originalWindow = global.window;
    delete (global as any).window;
    (global as any).window = Object.create(originalWindow || {});
    global.window.location = { href: '' } as any;

    vi.spyOn(auth, 'getSession').mockResolvedValue({
      accessToken: 'expired-token',
      role: 'Receptionist',
      tenantId: 't1',
      expiresAt: new Date(Date.now() - 10000).toISOString(),
    });

    server.use(
      http.get('http://localhost:5000/api/v1/protected', () => {
        return HttpResponse.json({
          status: 401,
          type: 'https://tools.ietf.org/html/rfc7235#section-3.1',
          detail: 'Unauthorized',
          traceId: 'test-trace'
        }, { status: 401 });
      })
    );

    await expect(apiFetch('/protected')).rejects.toThrow('Unauthorized');

    // Verify window.location.href was updated to redirect to /login
    expect(global.window.location.href).toBe('/login');
    
    global.window = originalWindow;
  });

  it('apiFetch_ParsesProblemDetailsErrors_IntoFieldMap', async () => {
    server.use(
      http.post('http://localhost:5000/api/v1/validate', () => {
        return HttpResponse.json({
          status: 400,
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
          title: 'One or more validation errors occurred.',
          detail: 'One or more validation errors occurred.',
          errors: {
            'Email': ['Email is required.', 'Email is invalid.'],
            'Password': ['Password is required.']
          },
          traceId: 'test-trace'
        }, { status: 400 });
      })
    );

    try {
      await apiFetch('/validate', { method: 'POST' });
      // Should throw, so force fail if it doesn't
      expect.fail('Should have thrown an ApiError');
    } catch (e: any) {
      expect(e.status).toBe(400);
      expect(e.errors).toEqual({
        'Email': ['Email is required.', 'Email is invalid.'],
        'Password': ['Password is required.']
      });
    }
  });
  it('apiFetch_On204_ReturnsEmptyObject', async () => {
    server.use(
      http.delete('http://localhost:5000/api/v1/delete', () => {
        return new HttpResponse(null, { status: 204 });
      })
    );
    const result = await apiFetch('/delete', { method: 'DELETE' });
    expect(result).toEqual({});
  });

  it('apiFetch_On200_ReturnsData', async () => {
    server.use(
      http.get('http://localhost:5000/api/v1/data', () => {
        return HttpResponse.json({ data: { message: 'success' } }, { status: 200 });
      })
    );
    const result = await apiFetch('/data');
    expect(result).toEqual({ message: 'success' });
  });

  it('apiFetch_NetworkError_ThrowsNetworkError', async () => {
    server.use(
      http.get('http://localhost:5000/api/v1/error', () => {
        return HttpResponse.error();
      })
    );
    await expect(apiFetch('/error')).rejects.toThrow('Failed to fetch');
  });
  it('apiFetch_On200_ReturnsDataWithoutDataProperty', async () => {
    server.use(
      http.get('http://localhost:5000/api/v1/nodata', () => {
        return HttpResponse.json([{ id: 1 }], { status: 200 });
      })
    );
    const result = await apiFetch('/nodata');
    expect(result).toEqual([{ id: 1 }]);
  });

  it('apiFetch_OnError_HandlesNonJsonErrorResponse', async () => {
    server.use(
      http.get('http://localhost:5000/api/v1/bad-json-error', () => {
        return new HttpResponse('<html>Server Error</html>', { status: 500, statusText: 'Internal Server Error' });
      })
    );
    try {
      await apiFetch('/bad-json-error');
      expect.fail('Should have thrown ApiError');
    } catch (e: any) {
      expect(e.status).toBe(500);
      expect(e.type).toBe('about:blank');
      expect(e.detail).toBe('Internal Server Error');
    }
  });
});

import { fetchAllPages } from './client';
describe('fetchAllPages', () => {
  it('fetches all pages until hasMore is false', async () => {
    const fetcher = vi.fn()
      .mockResolvedValueOnce({ items: [1, 2], nextCursor: 'c1', hasMore: true, count: 2 })
      .mockResolvedValueOnce({ items: [3], nextCursor: null, hasMore: false, count: 1 });
    const result = await fetchAllPages(fetcher);
    expect(result).toEqual([1, 2, 3]);
    expect(fetcher).toHaveBeenCalledTimes(2);
  });
});
