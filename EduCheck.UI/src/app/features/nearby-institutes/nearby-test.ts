import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LocationService, Coordinates } from '../../core/services/location.service';
import { NearbyInstitutesService } from '../../core/services/nearby-institutes.service';
import { MapViewComponent } from './components/map-view/map-view';
import { RadiusSelectorComponent, RadiusOption } from './components/radius-selector/radius-selector';
import { InstituteListComponent } from './components/institute-list/institute-list';
import { NearbyInstituteDto } from '../../core/models/pagination';

@Component({
  selector: 'app-nearby-test',
  standalone: true,
  imports: [CommonModule, MapViewComponent, RadiusSelectorComponent, InstituteListComponent],
  template: `
    <div class="nearby-test">
      <div class="nearby-test__header">
        <h2>📍 Nearby Institutes</h2>
        
        <button 
          class="btn-search" 
          (click)="searchNearby()"
          [disabled]="loading() || searching()"
        >
          {{ searching() ? 'Searching...' : loading() ? 'Getting location...' : 'Find Nearby Institutes' }}
        </button>
      </div>

      @if (error()) {
        <div class="error">{{ error() }}</div>
      }

      @if (coordinates()) {
        <div class="controls">
          <app-radius-selector
            [disabled]="searching"
            [resultsCount]="totalCount"
            (radiusChange)="onRadiusChange($event)"
          />
        </div>

        <div class="content-grid">
          <!-- Map Column -->
          <div class="map-column">
            <app-map-view
              [userLocation]="{ lat: coordinates()!.latitude, lng: coordinates()!.longitude }"
              [institutes]="institutes()"
              [zoom]="getZoomLevel()"
              (viewDetails)="onViewDetails($event)"
            />
          </div>

          <!-- List Column -->
          <div class="list-column">
            <app-institute-list
              [institutes]="institutes()"
              [loading]="searching"
              (selectInstitute)="onSelectInstitute($event)"
              (viewDetails)="onViewDetails($event)"
            />
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .nearby-test {
      padding: 24px;
      max-width: 1400px;
      margin: 0 auto;
      height: calc(100vh - 48px);
      display: flex;
      flex-direction: column;
    }

    .nearby-test__header {
      margin-bottom: 24px;

      h2 {
        margin: 0 0 16px 0;
        color: #1F2937;
      }
    }

    .btn-search {
      background: #3B82F6;
      color: white;
      border: none;
      padding: 14px 28px;
      border-radius: 8px;
      font-size: 16px;
      font-weight: 600;
      cursor: pointer;

      &:hover:not(:disabled) {
        background: #2563EB;
      }

      &:disabled {
        background: #9CA3AF;
        cursor: not-allowed;
      }
    }

    .controls {
      margin-bottom: 24px;
    }

    .error {
      padding: 16px;
      border-radius: 8px;
      margin-bottom: 24px;
      background: #FEE2E2;
      color: #991B1B;
    }

    .content-grid {
      display: grid;
      grid-template-columns: 1fr 400px;
      gap: 24px;
      flex: 1;
      min-height: 0;
    }

    .map-column,
    .list-column {
      border-radius: 12px;
      overflow: hidden;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }

    .map-column {
      min-height: 500px;
    }

    .list-column {
      background: white;
    }

    @media (max-width: 1024px) {
      .content-grid {
        grid-template-columns: 1fr;
        grid-template-rows: 400px auto;
      }

      .list-column {
        max-height: 500px;
      }
    }

    @media (max-width: 768px) {
      .nearby-test {
        padding: 16px;
        height: auto;
      }

      .content-grid {
        grid-template-rows: 350px auto;
        gap: 16px;
      }

      .list-column {
        max-height: 400px;
      }
    }
  `]
})
export class NearbyTestComponent {
  private locationService = inject(LocationService);
  private nearbyService = inject(NearbyInstitutesService);
  private router = inject(Router);

  loading = signal(false);
  searching = signal(false);
  error = signal<string | null>(null);
  coordinates = signal<Coordinates | null>(null);
  institutes = signal<NearbyInstituteDto[]>([]);
  totalCount = signal(0);
  radius = signal<RadiusOption>(10);

  async searchNearby() {
    this.loading.set(true);
    this.error.set(null);

    this.locationService.getCurrentLocation().subscribe({
      next: async (coords) => {
        this.coordinates.set(coords);
        this.loading.set(false);
        
        // Automatically search after getting location
        this.performSearch(coords.latitude, coords.longitude, this.radius());
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.message);
      }
    });
  }

  onRadiusChange(newRadius: RadiusOption) {
    this.radius.set(newRadius);
    const coords = this.coordinates();
    if (coords) {
      this.performSearch(coords.latitude, coords.longitude, newRadius);
    }
  }

  private performSearch(lat: number, lng: number, radius: number) {
    this.searching.set(true);
    
    this.nearbyService.getNearby({
      lat,
      lng,
      radius,
      page: 1,
      pageSize: 50 // Show max 50 markers
    }).subscribe({
      next: (response) => {
        this.institutes.set(response.data);
        this.totalCount.set(response.totalCount);
        this.searching.set(false);
        console.log(`Found ${response.totalCount} institutes within ${radius}km`);
      },
      error: (err) => {
        console.error('Search error:', err);
        this.error.set('Failed to search for nearby institutes');
        this.searching.set(false);
      }
    });
  }

  onViewDetails(instituteId: number) {
    console.log('Navigate to institute:', instituteId);
    this.router.navigate(['/institutes', instituteId]);
  }

  onSelectInstitute(institute: NearbyInstituteDto) {
    // Center map on selected institute (future enhancement)
    console.log('Selected institute:', institute);
  }

  /**
   * Adjust zoom level based on radius
   */
  getZoomLevel(): number {
    switch (this.radius()) {
      case 5: return 13;
      case 10: return 12;
      case 25: return 10;
      case 50: return 9;
      case 100: return 8;
      default: return 12;
    }
  }
}