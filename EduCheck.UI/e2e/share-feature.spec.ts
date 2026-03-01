import { test, expect } from '@playwright/test';
import { loginAsStudent } from './helpers/auth.helpers';

test.describe('Share Feature', () => {
  
  test.beforeEach(async ({ page }) => {
    await loginAsStudent(page);
    
    // Navigate to an institute detail page
    await page.goto('/search');
    await page.fill('input.search-box__input', 'Tambotie');
    await page.waitForTimeout(500); // Wait for debounce
    
    // Wait for results to load
    await page.waitForSelector('.institute-card', { state: 'visible', timeout: 10000 });
    
    // Click first result
    await page.locator('.institute-card').first().click();
    await page.waitForURL(/\/institutes\/\d+/, { timeout: 5000 });
    
    // Wait for detail page to load
    await page.waitForSelector('.detail-hero__name', { state: 'visible', timeout: 5000 });
  });

  test('should have share button visible on detail page', async ({ page }) => {
    const shareButton = page.locator('button.share-btn:has-text("Share")');
    await expect(shareButton).toBeVisible();
    await expect(shareButton).toBeEnabled();
  });

  test('should open share modal when clicking share button', async ({ page }) => {
    await page.click('button.share-btn:has-text("Share")');
    
    // Verify modal is visible
    await expect(page.locator('.share-modal')).toBeVisible();
    
    // Verify modal title
    await expect(page.locator('.modal-title')).toHaveText('Share');
    
    // Verify share options are visible
    await expect(page.locator('.share-option:has-text("Copy Link")')).toBeVisible();
    await expect(page.locator('.share-option:has-text("WhatsApp")')).toBeVisible();
    
    // Verify URL preview is visible
    await expect(page.locator('.url-preview')).toBeVisible();
    await expect(page.locator('.url-input')).toBeVisible();
  });

  test('should close modal when clicking cancel button', async ({ page }) => {
    await page.click('button.share-btn:has-text("Share")');
    
    // Verify modal is open
    await expect(page.locator('.share-modal')).toBeVisible();
    
    // Click cancel button
    await page.click('button.cancel-btn:has-text("Cancel")');
    
    // Verify modal is closed
    await expect(page.locator('.share-modal')).not.toBeVisible();
  });

  test('should close modal when clicking close icon', async ({ page }) => {
    await page.click('button.share-btn:has-text("Share")');
    
    // Verify modal is open
    await expect(page.locator('.share-modal')).toBeVisible();
    
    // Click close icon (✕)
    await page.click('button.close-icon');
    
    // Verify modal is closed
    await expect(page.locator('.share-modal')).not.toBeVisible();
  });

  test('should close modal when clicking overlay background', async ({ page }) => {
    await page.click('button.share-btn:has-text("Share")');
    
    // Verify modal is open
    await expect(page.locator('.share-modal')).toBeVisible();
    
    // Click overlay (outside modal) - click at top-left corner to ensure we hit overlay
    await page.locator('.modal-overlay').click({ position: { x: 10, y: 10 } });
    
    // Verify modal is closed
    await expect(page.locator('.share-modal')).not.toBeVisible();
  });

test('should display correct URL in preview field', async ({ page }) => {
  await page.click('button.share-btn:has-text("Share")');
  
  const currentUrl = page.url();
  const urlInput = page.locator('.url-input');
  const inputValue = await urlInput.inputValue();
  
  expect(inputValue).toBe(currentUrl);
  
  expect(inputValue).toContain('/institutes/');
  expect(inputValue).toMatch(/\/institutes\/\d+$/);
});

  test('should copy link to clipboard when clicking copy link', async ({ page, context }) => {
    // Grant clipboard permissions
    await context.grantPermissions(['clipboard-read', 'clipboard-write']);
    
    await page.click('button.share-btn:has-text("Share")');
    
    // Get expected URL before clicking
    const expectedUrl = page.url();
    
    // Click copy link option
    await page.click('.share-option:has-text("Copy Link")');
    
    // Read clipboard content
    const clipboardText = await page.evaluate(() => navigator.clipboard.readText());
    
    // Verify clipboard contains correct URL
    expect(clipboardText).toBe(expectedUrl);
    expect(clipboardText).toMatch(/https:\/\/staging.educheck.org.za:4200\/institutes\/\d+/);
  });

  test('should close modal and show success toast after copying link', async ({ page, context }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write']);
    
    await page.click('button.share-btn:has-text("Share")');
    await page.click('.share-option:has-text("Copy Link")');
    
    // Modal should close
    await expect(page.locator('.share-modal')).not.toBeVisible();
    
    // Success toast should appear
    await expect(page.locator('.toast')).toBeVisible();
    await expect(page.locator('.toast')).toContainText('Link copied to clipboard!');
    
    // Verify toast has success styling
    await expect(page.locator('.toast.toast--success')).toBeVisible();
  });

  test('should toast disappear after 3 seconds', async ({ page, context }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write']);
    
    await page.click('button.share-btn:has-text("Share")');
    await page.click('.share-option:has-text("Copy Link")');
    
    // Toast should be visible initially
    await expect(page.locator('.toast')).toBeVisible();
    
    // Wait for toast to disappear (3 seconds + small buffer)
    await page.waitForTimeout(3500);
    
    // Toast should be gone
    await expect(page.locator('.toast')).not.toBeVisible();
  });


  test('should close modal after clicking WhatsApp share', async ({ page }) => {
    await page.click('button.share-btn:has-text("Share")');
    
    // Modal should be visible
    await expect(page.locator('.share-modal')).toBeVisible();
    
    // Mock window.open to prevent actual navigation
    await page.evaluate(() => {
      window.open = () => null;
    });
    
    // Click WhatsApp button
    await page.click('.share-option:has-text("WhatsApp")');
    
    // Modal should close
    await expect(page.locator('.share-modal')).not.toBeVisible();
  });
});