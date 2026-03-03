import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LocationService, Coordinates } from '../../core/services/location.service';
import { NearbyInstitutesService } from '../../core/services/nearby-institutes.service';
import { AnalyticsService } from '../../core/services/analytics';
import { MapViewComponent } from './components/map-view/map-view';
import { RadiusSelectorComponent, RadiusOption } from './components/radius-selector/radius-selector';
import { InstituteListComponent } from './components/institute-list/institute-list';
import { NearbyInstituteDto } from '../../core/models/pagination';

@Component({
  selector: 'app-nearby-institutes',
  standalone: true,
  imports: [CommonModule, MapViewComponent, RadiusSelectorComponent, InstituteListComponent],
  templateUrl: './nearby-institutes.html',
  styleUrl: './nearby-institutes.scss'
})
export class NearbyInstitutesComponent implements OnInit {
  private locationService = inject(LocationService);
  private nearbyService = inject(NearbyInstitutesService);
  private router = inject(Router);
  private analytics = inject(AnalyticsService);

  loading = signal(false);
  searching = signal(false);
  error = signal<string | null>(null);
  coordinates = signal<Coordinates | null>(null);
  institutes = signal<NearbyInstituteDto[]>([]);
  totalCount = signal(0);
  radius = signal<RadiusOption>(10);
  hasSearched = signal(false);

  ngOnInit() {
    // Track page view
    this.analytics.trackPageView('nearby_institutes', {
      initial_radius: this.radius()
    });

    this.getLocationAndSearch();
  }

  async getLocationAndSearch() {
    this.loading.set(true);
    this.error.set(null);

    // Track geolocation request
    this.analytics.trackEvent('geolocation_permission_requested', {
      feature: 'nearby_institutes'
    });

    this.locationService.getCurrentLocation().subscribe({
      next: (coords) => {
        this.coordinates.set(coords);
        this.loading.set(false);
        this.hasSearched.set(true);

        // Track geolocation success
        this.analytics.trackEvent('geolocation_permission_granted', {
          feature: 'nearby_institutes',
          accuracy: coords.accuracy || 'unknown',
          latitude: coords.latitude,
          longitude: coords.longitude
        });
        
        this.performSearch(coords.latitude, coords.longitude, this.radius());
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.message);
        this.hasSearched.set(true);

        // Track geolocation denial/error
        this.analytics.trackEvent('geolocation_permission_denied', {
          feature: 'nearby_institutes',
          error_message: err.message,
          error_code: err.code
        });
      }
    });
  }

  onRadiusChange(newRadius: RadiusOption) {
    this.analytics.trackEvent('nearby_radius_changed', {
      previous_radius: this.radius(),
      new_radius: newRadius,
      current_results_count: this.institutes().length
    });

    this.radius.set(newRadius);
    const coords = this.coordinates();
    if (coords) {
      this.performSearch(coords.latitude, coords.longitude, newRadius);
    }
  }

  private performSearch(lat: number, lng: number, radius: number) {
    this.searching.set(true);

    // Track search initiation
    this.analytics.trackEvent('nearby_search_initiated', {
      latitude: lat,
      longitude: lng,
      radius_km: radius,
      has_coordinates: true
    });
    
    this.nearbyService.getNearby({
      lat,
      lng,
      radius,
      page: 1,
      pageSize: 50
    }).subscribe({
      next: (response) => {
        this.institutes.set(response.data);
        this.totalCount.set(response.totalCount);
        this.searching.set(false);

        const accreditedCount = response.data.filter(i => i.isAccredited).length;
        const unaccreditedCount = response.data.filter(i => !i.isAccredited).length;

        // Track search completion
        this.analytics.trackEvent('nearby_search_completed', {
          results_count: response.totalCount,
          displayed_count: response.data.length,
          radius_km: radius,
          latitude: lat,
          longitude: lng,
          accredited_count: accreditedCount,
          unaccredited_count: unaccreditedCount,
          has_results: response.totalCount > 0
        });

        // Track if no results found (potential fraud cluster)
        if (response.totalCount === 0) {
          this.analytics.trackEvent('nearby_no_results', {
            radius_km: radius,
            latitude: lat,
            longitude: lng,
            potential_fraud_cluster: true
          });
        }
      },
      error: (err) => {
        console.error('Search error:', err);
        this.error.set('Failed to search for nearby institutes. Please try again.');
        this.searching.set(false);

        // Track search error
        this.analytics.trackEvent('nearby_search_failed', {
          radius_km: radius,
          latitude: lat,
          longitude: lng,
          error_message: err?.message || 'Unknown error',
          error_status: err?.status
        });
      }
    });
  }

  onViewDetails(instituteId: number) {
    const institute = this.institutes().find(i => i.id === instituteId);

    this.analytics.trackEvent('nearby_institute_clicked', {
      institute_id: instituteId,
      institute_name: institute?.institutionName || 'unknown',
      distance_km: institute?.distance,
      is_accredited: institute?.isAccredited,
      radius_used: this.radius(),
      source: 'nearby_search'
    });

    this.router.navigate(['/institutes', instituteId]);
  }

  onSelectInstitute(institute: NearbyInstituteDto) {
    this.analytics.trackEvent('nearby_institute_selected', {
      institute_id: institute.id,
      institute_name: institute.institutionName,
      distance_km: institute.distance,
      is_accredited: institute.isAccredited
    });

    console.log('Selected institute:', institute);
  }

  retrySearch() {
    this.analytics.trackEvent('nearby_search_retry', {
      previous_error: this.error(),
      radius_km: this.radius()
    });

    this.error.set(null);
    this.getLocationAndSearch();
  }

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