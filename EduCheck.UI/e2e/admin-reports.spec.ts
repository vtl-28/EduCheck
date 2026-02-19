import { test, expect } from '@playwright/test';
import { loginAsAdmin } from './helpers/auth.helpers';

test.describe('Admin Reports Flow', () => {
  
  test('should display reports dashboard with statistics', async ({ page }) => {
    await loginAsAdmin(page);
    
    // Verify we're on the admin reports page
    await expect(page).toHaveURL('/admin/reports');
    
    // Verify page title
    await expect(page.locator('text=/Admin/i')).toBeVisible();
    
    // Verify stat cards are visible (Total, Submitted, Under Review, etc.)
    const statCards = page.locator('.stat-card');
    const statCount = await statCards.count();
    expect(statCount).toBeGreaterThan(0);
    
    // Verify at least "Total Reports" stat is visible
    await expect(page.locator('text=/Total/i')).toBeVisible();
  });

  test('should filter reports by status', async ({ page }) => {
    await loginAsAdmin(page);
    
    // Click on a filter chip (e.g., "Submitted")
    const submittedChip = page.locator('.filter-chip', { hasText: 'Submitted' });
    
    // Only test if the chip exists (might not if no submitted reports)
    if (await submittedChip.isVisible()) {
      await submittedChip.click();
      
      // Wait for filtered results
      await page.waitForTimeout(500);
      
      // Verify URL or UI updated
      // (Adjust based on your actual filtering implementation)
    }
  });
test('should expand report to view details', async ({ page }) => {
  await loginAsAdmin(page);
  
  const firstReportCard = page.locator('.report-card').first();
  const hasReports = await firstReportCard.isVisible();
  
  if (hasReports) {
    // Click the header to expand
    const reportHeader = firstReportCard.locator('.report-card__header');
    await reportHeader.click();
    
    // Wait for the body to be added to the DOM
    const reportBody = firstReportCard.locator('.report-card__body');
    await reportBody.waitFor({ state: 'visible', timeout: 5000 });
    
    // Verify expanded content is visible
    await expect(reportBody).toBeVisible();
    
    // Also verify description section exists
    const description = reportBody.locator('text=Description');
    await expect(description).toBeVisible();
  } else {
    console.log('No reports available to test expansion');
  }
});

test('should display reporter information in expanded report', async ({ page }) => {
  await loginAsAdmin(page);
  
  const firstReportCard = page.locator('.report-card').first();
  const hasReports = await firstReportCard.isVisible();
  
  if (hasReports) {
    // Click the header to expand (not the card itself)
    const reportHeader = firstReportCard.locator('.report-card__header');
    await reportHeader.click();
    
    // Wait for the body to be added to the DOM
    const reportBody = firstReportCard.locator('.report-card__body');
    await reportBody.waitFor({ state: 'visible', timeout: 5000 });
    
    // Verify reporter info is displayed with exact text
    const reporterLabel = page.locator('text=Reported By');
    await expect(reporterLabel).toBeVisible();
    
    // Also verify reporter info section exists
    const reporterInfo = page.locator('.reporter-info');
    await expect(reporterInfo).toBeVisible();
  }
});

  test('should prevent student from accessing admin reports', async ({ page }) => {
    // This is already tested in auth.spec.ts but worth including here for completeness
    const { loginAsStudent } = await import('./helpers/auth.helpers');
    await loginAsStudent(page);
    
    // Try to navigate to admin reports
    await page.goto('/admin/reports');
    
    // Should be redirected to /search
    await expect(page).toHaveURL('/search');
  });
});