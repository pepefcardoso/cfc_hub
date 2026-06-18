import { test as base, expect } from '@playwright/test';

type AuthFixtures = {
  loginAs: (email: string, password?: string) => Promise<void>;
};

export const test = base.extend<AuthFixtures>({
  loginAs: async ({ context }, use) => {
    await use(async (email: string, password = 'password') => {
      // By using context.request, the Set-Cookie headers from Next.js 
      // will be automatically applied to the browser context.
      const response = await context.request.post('/api/auth/login', {
        data: { email, password },
      });
      
      expect(response.ok()).toBeTruthy();
    });
  },
});

export { expect };
