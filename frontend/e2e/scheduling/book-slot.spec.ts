import { test, expect } from '../fixtures/test';

test.describe('Book Slot Flow', () => {
  test.beforeEach(async ({ apiHelper, loginAs, tenantContext }) => {
    await apiHelper.seedSlot(tenantContext.slug);
    await loginAs(`admin@${tenantContext.slug}.com`, process.env.TEST_ADMIN_PASSWORD || 'password');
  });

  test('BookSlot_HappyPath_SlotAppearsInCalendar', async ({ page }) => {
    await page.goto('/agenda');
    
    // Open booking dialog
    await page.click('button:has-text("Agendar")');
    
    // Select student, vehicle, track
    await page.fill('input[name="student"]', 'John Doe');
    await page.click('text=John Doe'); // Select from dropdown
    
    // Submit form
    await page.click('button:has-text("Confirmar")');
    
    // Expect success toast and calendar updated
    await expect(page.locator('text=Agendamento confirmado')).toBeVisible();
    await expect(page.locator('.fc-event')).toBeVisible(); // FullCalendar event
  });

  test('BookSlot_WhenConflict_ShowsInlineErrorAndKeepsDialogOpen', async ({ page }) => {
    await page.goto('/agenda');
    
    await page.click('button:has-text("Agendar")');
    
    // Intentionally create conflict
    await page.fill('input[name="student"]', 'Jane Doe');
    await page.click('button:has-text("Confirmar")');
    
    // Expect error message inline and dialog to remain open
    await expect(page.locator('text=Conflito de agendamento')).toBeVisible();
    await expect(page.locator('div[role="dialog"]')).toBeVisible();
  });
});
