import { Page } from '@playwright/test';

/**
 * Test user credentials
 * These should match users created in your test database or seed data
 */
export const TEST_USERS = {
  student: {
    email: 'student.test@educheck.co.za',
    password: 'Test@123456',
    firstName: 'Test',
    lastName: 'Student',
    province: 'Gauteng',
    city: 'Johannesburg',
  },
  admin: {
    email: 'admin.test@educheck.co.za',
    password: 'Admin@123456',
    firstName: 'Test',
    lastName: 'Admin',
  },
};

/**
 * Register a new student account
 */
export async function registerStudent(page: Page, user = TEST_USERS.student) {
  await page.goto('/auth/register');
  
  // Wait for form to be ready
  await page.waitForSelector('input[formControlName="firstName"]', { state: 'visible' });
  
  await page.fill('input[formControlName="firstName"]', user.firstName);
  await page.fill('input[formControlName="lastName"]', user.lastName);
  await page.fill('input[formControlName="email"]', user.email);
  await page.fill('input[formControlName="password"]', user.password);
  await page.fill('input[formControlName="confirmPassword"]', user.password);
  
  if (user.province) {
    await page.selectOption('select[formControlName="province"]', user.province);
  }
  
  if (user.city) {
    await page.fill('input[formControlName="city"]', user.city);
  }
  
  await page.click('button[type="submit"]');
  
  // Wait for redirect to search page
  await page.waitForURL('/search', { timeout: 15000 });
}

/**
 * Login as a student
 */
export async function loginAsStudent(page: Page, user = TEST_USERS.student) {
  await page.goto('/auth/login');
  
  // Wait for login form to be fully loaded and interactive
  await page.waitForSelector('input[formControlName="email"]', { state: 'visible' });
  await page.waitForLoadState('networkidle');
  
  // Fill credentials
  await page.fill('input[formControlName="email"]', user.email);
  await page.fill('input[formControlName="password"]', user.password);
  
  // Click submit and wait for navigation
  await Promise.all([
    page.waitForURL('/search', { timeout: 15000 }),
    page.click('button[type="submit"]'),
  ]);
}

/**
 * Login as an admin
 */
export async function loginAsAdmin(page: Page, user = TEST_USERS.admin) {
  await page.goto('/auth/login');
  
  // Wait for login form to be fully loaded and interactive
  await page.waitForSelector('input[formControlName="email"]', { state: 'visible' });
  await page.waitForLoadState('networkidle');
  
  // Fill credentials
  await page.fill('input[formControlName="email"]', user.email);
  await page.fill('input[formControlName="password"]', user.password);
  
  // Click submit and wait for navigation
  await Promise.all([
    page.waitForURL('/admin/reports', { timeout: 15000 }),
    page.click('button[type="submit"]'),
  ]);
}

/**
 * Logout
 * This function handles opening the drawer and user menu if needed
 */
export async function logout(page: Page) {
  // Check if drawer is already open by looking for the drawer container
  const drawerOpen = await page.locator('.drawer-container--open').isVisible();
  
  if (!drawerOpen) {
    // Open the drawer
    await page.click('button.app-bar__menu-btn');
    await page.waitForSelector('.drawer-container--open', { state: 'visible', timeout: 5000 });
  }
  
  // Click the user pill at the bottom of the drawer to open user menu
  await page.click('.user-pill');
  
  // Wait for user menu to appear
  await page.waitForSelector('.user-menu', { state: 'visible', timeout: 5000 });
  
  // Click the logout option in the user menu
  await page.click('.user-menu__item--danger');
  
  // Wait for redirect to landing or login page
  await page.waitForURL(/\/(auth\/login)?$/, { timeout: 5000 });
}