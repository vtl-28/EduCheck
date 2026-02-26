import { Injectable } from '@angular/core';
import { Observable, Observer } from 'rxjs';

export interface Coordinates {
  latitude: number;
  longitude: number;
  accuracy?: number;
}

export interface LocationError {
  code: number;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  
  /**
   * Get user's current location using browser Geolocation API
   * Tries high accuracy first, falls back to network location if timeout
   * @returns Observable that emits coordinates or error
   */
  getCurrentLocation(): Observable<Coordinates> {
    return new Observable((observer: Observer<Coordinates>) => {
      // Check if geolocation is supported
      if (!navigator.geolocation) {
        observer.error({
          code: 0,
          message: 'Geolocation is not supported by your browser'
        } as LocationError);
        return;
      }

      // Try high accuracy first (GPS)
      console.log('Attempting high-accuracy location...');
      navigator.geolocation.getCurrentPosition(
        // Success with high accuracy
        (position) => {
          console.log('High-accuracy location success');
          const coords: Coordinates = {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy
          };
          observer.next(coords);
          observer.complete();
        },
        // Failed with high accuracy - try fallback
        (error) => {
          console.warn('High-accuracy failed, trying fallback...', error);
          
          // Fallback: Try network-based location (lower accuracy but faster)
          navigator.geolocation.getCurrentPosition(
            (position) => {
              console.log('Fallback location success');
              const coords: Coordinates = {
                latitude: position.coords.latitude,
                longitude: position.coords.longitude,
                accuracy: position.coords.accuracy
              };
              observer.next(coords);
              observer.complete();
            },
            (fallbackError) => {
              console.error('Both location attempts failed', fallbackError);
              const locationError: LocationError = {
                code: fallbackError.code,
                message: this.getErrorMessage(fallbackError.code)
              };
              observer.error(locationError);
            },
            {
              enableHighAccuracy: false, // Use network location
              timeout: 15000,
              maximumAge: 300000 // Allow 5-minute-old cached position
            }
          );
        },
        {
          enableHighAccuracy: true, // Try GPS first
          timeout: 5000, // Short timeout for GPS
          maximumAge: 0 // No cache for high accuracy
        }
      );
    });
  }

  /**
   * Watch user's location (continuous updates)
   * Useful for tracking movement
   */
  watchLocation(): Observable<Coordinates> {
    return new Observable((observer: Observer<Coordinates>) => {
      if (!navigator.geolocation) {
        observer.error({
          code: 0,
          message: 'Geolocation is not supported by your browser'
        } as LocationError);
        return;
      }

      const watchId = navigator.geolocation.watchPosition(
        (position) => {
          observer.next({
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
            accuracy: position.coords.accuracy
          });
        },
        (error) => {
          observer.error({
            code: error.code,
            message: this.getErrorMessage(error.code)
          });
        },
        {
          enableHighAccuracy: true,
          timeout: 10000,
          maximumAge: 0
        }
      );

      // Cleanup function
      return () => {
        navigator.geolocation.clearWatch(watchId);
      };
    });
  }

  /**
   * Get user-friendly error message based on error code
   */
  private getErrorMessage(code: number): string {
    switch (code) {
      case 1: // PERMISSION_DENIED
        return 'Location access denied. Please enable location permissions in your browser settings.';
      case 2: // POSITION_UNAVAILABLE
        return 'Location information is unavailable. Please check your GPS or network connection.';
      case 3: // TIMEOUT
        return 'Location request timed out. Please try again.';
      default:
        return 'An unknown error occurred while getting your location.';
    }
  }

  /**
   * Check if geolocation is supported by browser
   */
  isSupported(): boolean {
    return 'geolocation' in navigator;
  }
}