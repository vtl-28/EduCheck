export const environment = {
  production: true,
  apiUrl: 'https://staging.educheck.org.za/api',
  googleMapsApiKey: 'YOUR_GOOGLE_MAPS_API_KEY_HERE',
  posthog: {
    apiKey: 'phc_YOUR_DEV_KEY_HERE',  // Replace with your actual key
    apiHost: 'https://app.posthog.com',
    enabled: true,  // Set to false to disable in dev if needed
    autocapture: true,
    capturePageViews: true
  }
};