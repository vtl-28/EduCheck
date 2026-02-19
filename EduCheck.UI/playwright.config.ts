/// <reference types="node" />
import { defineConfig, devices } from '@playwright/test';

const CI = !!process.env['CI'];

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  testDir: './e2e',
  
  /* Maximum time one test can run for */
  timeout: 30 * 1000,
  
  /* Run tests in files in parallel */
  //fullyParallel: true,
  
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: CI,
  
  /* Retry on CI only */
  retries: CI ? 2 : 0,
  
  /* Opt out of parallel tests on CI. */
  workers: CI ? 1 : undefined,
  
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: CI 
    ? [['html'], ['github'], ['junit', { outputFile: 'e2e-results/results.xml' }]]
    : 'html',
  
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: process.env['BASE_URL'] || 'http://localhost:4200',
    //headless: false,
    
    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    
    /* Screenshot only on failure */
    screenshot: 'only-on-failure',
    
    /* Video only on retry */
    video: 'retain-on-failure',
    
    /* Maximum time each action such as `click()` can take */
    actionTimeout: 10 * 1000,
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    //Run Firefox and WebKit only in CI to save time locally
    ...(CI ? [
      {
        name: 'firefox',
        use: { ...devices['Desktop Firefox'] },
      },
      {
        name: 'webkit',
        use: { ...devices['Desktop Safari'] },
      },
    ] : []),
  ],

  /* Run your local dev server before starting the tests */
  webServer: CI ? undefined : {
    command: 'docker-compose up',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 120 * 1000, // 2 minutes for docker-compose to start both services
    cwd: '..',
  },
});