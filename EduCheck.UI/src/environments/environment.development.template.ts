export const environment = {
  production: false,
  apiUrl: 'http://localhost:5169/api',
  googleMapsApiKey: 'YOUR_GOOGLE_MAPS_API_KEY_HERE',
  posthog: {
    apiKey: 'phc_Czmx8UKtqEVOmdDU95fRGdLOlxmBn4xICEDGI2nPfIM',  // Replace with your actual key
    apiHost: 'https://app.posthog.com',
    enabled: true,  // Set to false to disable in dev if needed
    autocapture: true,
    capturePageViews: true
  }
};