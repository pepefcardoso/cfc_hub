import { test, expect } from '../fixtures/test';

test.describe('Create Student Flow', () => {
  test.beforeEach(async ({ loginAs, tenantContext }) => {
    await loginAs(`admin@${tenantContext.slug}.com`, process.env.TEST_ADMIN_PASSWORD || 'password');
  });

  test('CreateStudent_WithConsent_RedirectsToStudentDetail', async ({ page }) => {
    await page.goto('/students/new');
    
    await page.fill('input[name="name"]', 'Test Student');
    await page.fill('input[name="cpf"]', '12345678901');
    await page.fill('input[name="email"]', 'student@test.com');
    
    // Give LGPD consent
    await page.check('input[name="lgpdConsent"]');
    
    await page.click('button[type="submit"]');
    
    // Should redirect to details page
    await expect(page).toHaveURL(/.*\/students\/.+/);
    await expect(page.locator('text=Test Student')).toBeVisible();
  });

  test('CreateStudent_WithoutConsentCheckbox_BlocksSubmission', async ({ page }) => {
    await page.goto('/students/new');
    
    await page.fill('input[name="name"]', 'Test Student');
    await page.fill('input[name="cpf"]', '12345678901');
    
    // Do NOT check consent
    await page.click('button[type="submit"]');
    
    // Should show error and stay on page
    await expect(page.locator('text=Consentimento é obrigatório')).toBeVisible();
    await expect(page).toHaveURL(/.*\/students\/new/);
  });

  test('CreateStudent_WithDuplicateCpf_ShowsInlineErrorOnStep1', async ({ page }) => {
    // Seed first student via UI to ensure duplicate CPF exists
    await page.goto('/students/new');
    await page.fill('input[name="name"]', 'First Student');
    await page.fill('input[name="cpf"]', '00000000000');
    await page.fill('input[name="email"]', 'first@test.com');
    await page.check('input[name="lgpdConsent"]');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL(/.*\/students\/.+/);

    // Attempt to create duplicate
    await page.goto('/students/new');
    
    await page.fill('input[name="name"]', 'Duplicate CPF');
    await page.fill('input[name="cpf"]', '00000000000'); 
    await page.check('input[name="lgpdConsent"]');
    
    await page.click('button[type="submit"]');
    
    // Should show conflict error inline
    await expect(page.locator('text=CPF já cadastrado')).toBeVisible();
  });
});
