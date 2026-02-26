import { Component, signal, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LocationService, Coordinates } from '../../core/services/location.service';
import { NearbyInstitutesService } from '../../core/services/nearby-institutes.service';
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


  loading = signal(false);
  searching = signal(false);
  error = signal<string | null>(null);
  coordinates = signal<Coordinates | null>(null);
  institutes = signal<NearbyInstituteDto[]>([]);
  totalCount = signal(0);
  radius = signal<RadiusOption>(10);
  hasSearched = signal(false);

  ngOnInit() {
    this.getLocationAndSearch();
  }

  async getLocationAndSearch() {
    this.loading.set(true);
    this.error.set(null);

    this.locationService.getCurrentLocation().subscribe({
      next: (coords) => {
        this.coordinates.set(coords);
        this.loading.set(false);
        this.hasSearched.set(true);
        
        this.performSearch(coords.latitude, coords.longitude, this.radius());
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.message);
        this.hasSearched.set(true);
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
      pageSize: 50
    }).subscribe({
      next: (response) => {
        this.institutes.set(response.data);
        this.totalCount.set(response.totalCount);
        this.searching.set(false);
      },
      error: (err) => {
        console.error('Search error:', err);
        this.error.set('Failed to search for nearby institutes. Please try again.');
        this.searching.set(false);
      }
    });
  }

  onViewDetails(instituteId: number) {
    this.router.navigate(['/institutes', instituteId]);
  }

  onSelectInstitute(institute: NearbyInstituteDto) {
    console.log('Selected institute:', institute);
  }

  retrySearch() {
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