import { test, expect } from '@playwright/test';
import { loginAsStudent, loginAsAdmin, logout } from './helpers/auth.helpers';

test.describe('Authentication Flows', () => {
  
  test('should allow student to login with valid credentials', async ({ page }) => {
    await loginAsStudent(page);
    
    // Verify redirected to search page
    await expect(page).toHaveURL('/search');
    
    // Verify search interface is visible
    await expect(page.locator('input[placeholder*="Search institution name..."]')).toBeVisible();
  });

  test('should reject invalid login credentials', async ({ page }) => {
    await page.goto('/auth/login');
    
    await page.fill('input[formControlName="email"]', 'wrong@example.com');
    await page.fill('input[formControlName="password"]', 'WrongPassword123');
    
    await page.click('button[type="submit"]');
    
    // Should stay on login page
    await expect(page).toHaveURL('/auth/login');
    
    // Should show error message (snackbar or inline error)
    await expect(page.locator('text=/Invalid|incorrect/i')).toBeVisible({ timeout: 5000 });
  });

  test('should allow student to logout', async ({ page }) => {
    await loginAsStudent(page);
    
    // logout() helper will handle opening drawer and user menu
    await logout(page);
    
    // Verify redirected to landing or login page
    await expect(page.url()).toMatch(/\/(auth\/login)?$/);
  });

  test('should redirect admin to /admin/reports after login', async ({ page }) => {
    await loginAsAdmin(page);
    
    // Verify redirected to admin reports page, not /search
    await expect(page).toHaveURL('/admin/reports');
    
    // Verify admin interface elements are visible
    await expect(page.locator('text=/Admin/i')).toBeVisible();
  });

  test('should redirect unauthenticated user to login when accessing protected route', async ({ page }) => {
    // Try to access search page without logging in
    await page.goto('/search');
    
    // Should be redirected to login with returnUrl
    await expect(page).toHaveURL(/\/auth\/login\?returnUrl=/);
  });

  test('should prevent student from accessing admin routes', async ({ page }) => {
    await loginAsStudent(page);
    
    // Try to access admin page
    await page.goto('/admin/reports');
    
    // Should be redirected back to search page
    await expect(page).toHaveURL('/search');
  });

  test('should redirect already logged-in user away from login page', async ({ page }) => {
    await loginAsStudent(page);
    
    // Try to go back to login
    await page.goto('/auth/login');
    
    // Should be redirected to search (guest guard)
    await expect(page).toHaveURL('/search');
  });
});