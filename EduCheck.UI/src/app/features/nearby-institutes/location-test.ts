import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocationService, Coordinates, LocationError } from '../../core/services/location.service';
import { MapViewComponent } from './components/map-view/map-view';

@Component({
  selector: 'app-location-test',
  standalone: true,
  imports: [CommonModule, MapViewComponent],
  template: `
    <div class="location-test">
      <h2>📍 Location Test</h2>
      
      <button 
        class="btn-get-location" 
        (click)="getLocation()"
        [disabled]="loading()"
      >
        {{ loading() ? 'Getting location...' : 'Get My Location' }}
      </button>

      <button 
        class="btn-test-location" 
        (click)="useTestLocation()"
        [disabled]="loading()"
      >
        📍 Use Test Location (Johannesburg)
      </button>

      @if (loading()) {
        <div class="loading">
          <div class="spinner"></div>
          <p>Requesting location permission...</p>
        </div>
      }

      @if (coordinates()) {
        <div class="success">
          <h3>✅ Location Retrieved!</h3>
          <div class="coords">
            <p><strong>Latitude:</strong> {{ coordinates()!.latitude }}</p>
            <p><strong>Longitude:</strong> {{ coordinates()!.longitude }}</p>
            <p>
              <strong>Accuracy:</strong> {{ formatAccuracy(coordinates()!.accuracy!) }}
              @if (getAccuracyLevel(coordinates()!.accuracy!) === 'poor') {
                <span class="accuracy-badge accuracy-badge--poor">
                  Poor (Desktop limitation)
                </span>
              } @else if (getAccuracyLevel(coordinates()!.accuracy!) === 'medium') {
                <span class="accuracy-badge accuracy-badge--medium">
                  Medium
                </span>
              } @else {
                <span class="accuracy-badge accuracy-badge--good">
                  Good
                </span>
              }
            </p>
          </div>
          
          @if (getAccuracyLevel(coordinates()!.accuracy!) === 'poor') {
            <div class="accuracy-warning">
              <p><strong>⚠️ Low Accuracy Detected</strong></p>
              <p>Desktop browsers use IP-based location, which can be 50-500 km off.</p>
              <p><strong>For accurate results:</strong></p>
              <ul>
                <li>Use a mobile device with GPS</li>
                <li>Or manually search for your location (future feature)</li>
              </ul>
            </div>
          }
          
          <p class="info">
            This is your current location. We'll use this to find nearby institutes.
          </p>

          <!-- Map display -->
          <div class="map-display">
            <h4>📍 Your Location on Map</h4>
            <app-map-view
              [userLocation]="{ lat: coordinates()!.latitude, lng: coordinates()!.longitude }"
              [zoom]="12"
            />
          </div>
        </div>
      }

      @if (error()) {
        <div class="error">
          <h3>❌ Error</h3>
          <p>{{ error()!.message }}</p>
          @if (error()!.code === 1) {
            <div class="help">
              <p><strong>How to fix:</strong></p>
              <ol>
                <li>Click the location icon in your browser's address bar</li>
                <li>Select "Allow" for location access</li>
                <li>Click "Get My Location" again</li>
              </ol>
            </div>
          }
        </div>
      }

      @if (!locationService.isSupported()) {
        <div class="warning">
          ⚠️ Your browser doesn't support geolocation
        </div>
      }
    </div>
  `,
  styles: [`
    .location-test {
      max-width: 600px;
      margin: 40px auto;
      padding: 24px;
      font-family: system-ui, -apple-system, sans-serif;
    }

    h2 {
      font-size: 24px;
      margin-bottom: 24px;
      color: #1F2937;
    }

    .btn-get-location {
      background: #3B82F6;
      color: white;
      border: none;
      padding: 14px 28px;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      width: 100%;
      max-width: 300px;
    }

    .btn-get-location:hover:not(:disabled) {
      background: #2563EB;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
    }

    .btn-get-location:disabled {
      background: #9CA3AF;
      cursor: not-allowed;
    }

    .btn-test-location {
      background: #10B981;
      color: white;
      border: none;
      padding: 14px 28px;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
      width: 100%;
      max-width: 300px;
      margin-top: 12px;
    }

    .btn-test-location:hover:not(:disabled) {
      background: #059669;
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(16, 185, 129, 0.3);
    }

    .btn-test-location:disabled {
      background: #9CA3AF;
      cursor: not-allowed;
    }

    .loading {
      margin-top: 24px;
      text-align: center;
      padding: 32px;
      background: #EFF6FF;
      border-radius: 12px;
    }

    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid #DBEAFE;
      border-top-color: #3B82F6;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin: 0 auto 16px;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .success {
      margin-top: 24px;
      padding: 24px;
      background: #ECFDF5;
      border: 2px solid #10B981;
      border-radius: 12px;
    }

    .success h3 {
      color: #059669;
      margin: 0 0 16px 0;
    }

    .coords {
      background: white;
      padding: 16px;
      border-radius: 8px;
      margin-bottom: 16px;
    }

    .coords p {
      margin: 8px 0;
      font-family: 'Courier New', monospace;
      color: #374151;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .accuracy-badge {
      display: inline-block;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 600;
      font-family: system-ui, -apple-system, sans-serif;
      
      &--good {
        background: #D1FAE5;
        color: #065F46;
      }
      
      &--medium {
        background: #FEF3C7;
        color: #92400E;
      }
      
      &--poor {
        background: #FEE2E2;
        color: #991B1B;
      }
    }

    .accuracy-warning {
      margin-top: 16px;
      padding: 16px;
      background: #FEF3C7;
      border: 2px solid #F59E0B;
      border-radius: 8px;
      
      p {
        color: #92400E;
        margin: 8px 0;
        font-family: system-ui, -apple-system, sans-serif;
        
        &:first-child {
          font-weight: 600;
          margin-top: 0;
        }
      }
      
      ul {
        margin: 8px 0 0 0;
        padding-left: 24px;
        color: #78350F;
        
        li {
          margin: 4px 0;
        }
      }
    }

    .info {
      color: #059669;
      font-size: 14px;
      margin: 0;
    }

    .map-display {
      margin-top: 24px;
    }

    .map-display h4 {
      margin: 0 0 12px 0;
      color: #059669;
      font-size: 16px;
    }

    .map-display app-map-view {
      display: block;
      height: 400px;
      border-radius: 8px;
      overflow: hidden;
    }

    .error {
      margin-top: 24px;
      padding: 24px;
      background: #FEF2F2;
      border: 2px solid #EF4444;
      border-radius: 12px;
    }

    .error h3 {
      color: #DC2626;
      margin: 0 0 12px 0;
    }

    .error p {
      color: #991B1B;
      margin: 8px 0;
    }

    .help {
      margin-top: 16px;
      padding: 16px;
      background: white;
      border-radius: 8px;
    }

    .help p {
      color: #374151;
      font-weight: 600;
      margin: 0 0 8px 0;
    }

    .help ol {
      margin: 8px 0;
      padding-left: 24px;
      color: #4B5563;
    }

    .help li {
      margin: 4px 0;
    }

    .warning {
      margin-top: 24px;
      padding: 16px;
      background: #FEF3C7;
      border: 2px solid #F59E0B;
      border-radius: 8px;
      color: #92400E;
      font-weight: 500;
    }
  `]
})
export class LocationTestComponent {
  loading = signal(false);
  coordinates = signal<Coordinates | null>(null);
  error = signal<LocationError | null>(null);

  constructor(public locationService: LocationService) {}

  getLocation() {
    this.loading.set(true);
    this.error.set(null);
    this.coordinates.set(null);

    this.locationService.getCurrentLocation().subscribe({
      next: (coords) => {
        this.loading.set(false);
        this.coordinates.set(coords);
        console.log('Location retrieved:', coords);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err);
        console.error('Location error:', err);
      }
    });
  }

  useTestLocation() {
    // Johannesburg city center coordinates
    const testCoords: Coordinates = {
      latitude: -26.2041,
      longitude: 28.0473,
      accuracy: 100
    };
    this.coordinates.set(testCoords);
    this.error.set(null);
    console.log('Using test location:', testCoords);
  }

  /**
   * Format accuracy in human-readable form
   */
  formatAccuracy(accuracy: number): string {
    if (accuracy >= 1000) {
      return `${(accuracy / 1000).toFixed(1)} km`;
    }
    return `${Math.round(accuracy)} meters`;
  }

  /**
   * Get accuracy level based on meters
   */
  getAccuracyLevel(accuracy: number): 'good' | 'medium' | 'poor' {
    if (accuracy <= 1000) return 'good';      // < 1km
    if (accuracy <= 10000) return 'medium';   // 1-10km
    return 'poor';                             // > 10km
  }
}