import { apiFetch } from './client';
import * as auth from './auth';

// Mock fetch globally
const originalFetch = global.fetch;

describe('apiFetch', () => {
  beforeEach(() => {
    global.fetch = jest.fn();
    // clear memory cache
    auth.clearSessionCache();
  });

  afterEach(() => {
    global.fetch = originalFetch;
    jest.resetAllMocks();
  });

  it('apiFetch_WithExpiredToken_RedirectsToLogin', async () => {
    // Setup window object to allow mocking window.location.href
    const originalWindow = global.window;
    delete (global as any).window;
    (global as any).window = Object.create(originalWindow || {});
    global.window.location = { href: '' } as any;

    const mockFetch = global.fetch as jest.Mock;
    
    // getSession will fetch /api/auth/session and return null if expired, or return session
    // let's mock it to return an expired session or simply let backend return 401
    jest.spyOn(auth, 'getSession').mockResolvedValue({
      accessToken: 'expired-token',
      role: 'Receptionist',
      tenantId: 't1',
      expiresAt: new Date(Date.now() - 10000).toISOString(),
    });

    // Mock API response as 401 Unauthorized
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 401,
      statusText: 'Unauthorized',
      json: async () => ({
        status: 401,
        type: 'https://tools.ietf.org/html/rfc7235#section-3.1',
        detail: 'Unauthorized',
        traceId: 'test-trace'
      })
    });

    try {
      await apiFetch('/some-endpoint');
    } catch (e) {
      // It throws ApiError due to !response.ok
    }

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/some-endpoint'),
      expect.objectContaining({
        headers: expect.any(Headers)
      })
    );

    // Verify window.location.href was updated to redirect to /login
    expect(global.window.location.href).toBe('/login');
    
    // Restore window
    global.window = originalWindow;
  });
});
