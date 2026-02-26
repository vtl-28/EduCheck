import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class GoogleMapsLoaderService {
  private static promise: Promise<void> | null = null;

  /**
   * Load Google Maps JavaScript API dynamically
   * Only loads once, subsequent calls return the same promise
   */
  load(): Promise<void> {
    // If already loaded or loading, return existing promise
    if (GoogleMapsLoaderService.promise) {
      return GoogleMapsLoaderService.promise;
    }

    // If already loaded by another means, resolve immediately
    if (typeof google !== 'undefined' && google.maps) {
      return Promise.resolve();
    }

    GoogleMapsLoaderService.promise = new Promise<void>((resolve, reject) => {
      const script = document.createElement('script');
      script.src = `https://maps.googleapis.com/maps/api/js?key=${environment.googleMapsApiKey}&libraries=places&loading=async`;
      script.async = true;
      script.defer = true;

      script.onload = () => {
        console.log('Google Maps API loaded successfully');
        resolve();
      };

      script.onerror = (error) => {
        console.error('Failed to load Google Maps API', error);
        GoogleMapsLoaderService.promise = null;
        reject(error);
      };

      document.head.appendChild(script);
    });

    return GoogleMapsLoaderService.promise;
  }

  /**
   * Check if Google Maps API is loaded
   */
  isLoaded(): boolean {
    return typeof google !== 'undefined' && typeof google.maps !== 'undefined';
  }
}