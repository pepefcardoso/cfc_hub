import { http, HttpResponse } from 'msw';

export const handlers = [
  // Fallback handler for all API requests to return a generic 500
  // Tests should override these using server.use()
  http.all('/api/*', () => {
    return HttpResponse.json({ error: 'Unhandled request in MSW' }, { status: 500 });
  }),
];
