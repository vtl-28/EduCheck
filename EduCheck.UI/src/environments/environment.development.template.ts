export const environment = {
  production: false,
  apiUrl: 'http://localhost:5169/api',
  googleMapsApiKey: 'YOUR_GOOGLE_MAPS_API_KEY_HERE',
  posthog: {
    apiKey: 'phc_YOUR_DEV_KEY_HERE',
    apiHost: 'https://app.posthog.com',
    enabled: true,
    autocapture: true,
    capturePageViews: true
  }
};