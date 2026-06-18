import { test, expect } from '../fixtures/test';

test.describe('Login Flow', () => {
  test('Login_WithValidCredentials_RedirectsToAgenda', async ({ page }) => {
    await page.goto('/login');
    
    // Fill credentials
    await page.fill('input[name="email"]', process.env.TEST_USER_EMAIL || 'test@example.com');
    await page.fill('input[name="password"]', process.env.TEST_USER_PASSWORD || 'password');
    
    // Click submit
    await page.click('button[type="submit"]');
    
    // Expect redirection to agenda (or dashboard)
    await expect(page).toHaveURL(/.*\/agenda/);
  });

  test('Login_WithInvalidPassword_ShowsErrorInline', async ({ page }) => {
    await page.goto('/login');
    
    await page.fill('input[name="email"]', process.env.TEST_USER_EMAIL || 'test@example.com');
    await page.fill('input[name="password"]', 'wrongpassword');
    
    await page.click('button[type="submit"]');
    
    // Check for inline error message
    const errorMessage = page.locator('text=Falha na autenticação');
    await expect(errorMessage).toBeVisible();
  });

  test('Login_WithInactiveAccount_ShowsForbiddenMessage', async ({ page }) => {
    await page.goto('/login');
    
    await page.fill('input[name="email"]', process.env.TEST_INACTIVE_EMAIL || 'inactive@cfc.com');
    await page.fill('input[name="password"]', process.env.TEST_USER_PASSWORD || 'password');
    
    await page.click('button[type="submit"]');
    
    // Check for forbidden message
    const forbiddenMessage = page.locator('text=Conta inativa'); // or whatever the text is
    await expect(forbiddenMessage).toBeVisible();
  });
});
