import { Component, Input, OnInit, ViewChild, signal, output } from '@angular/core';
import { GoogleMap, MapMarker, MapInfoWindow, GoogleMapsModule } from '@angular/google-maps';
import { CommonModule } from '@angular/common';
import { GoogleMapsLoaderService } from '../../../../core/services/google-maps-loader.service';
import { NearbyInstituteDto } from '../../../../core/models/pagination';

export interface MapLocation {
  lat: number;
  lng: number;
}

@Component({
  selector: 'app-map-view',
  standalone: true,
  imports: [CommonModule, GoogleMapsModule],
  templateUrl: './map-view.html',
  styleUrl: './map-view.scss'
})
export class MapViewComponent implements OnInit {
  @ViewChild(GoogleMap) map!: GoogleMap;
  @ViewChild(MapInfoWindow) infoWindow!: MapInfoWindow;
  
  @Input() userLocation: MapLocation | null = null;
  @Input() institutes: NearbyInstituteDto[] = [];
  @Input() zoom: number = 12;
  
  
  viewDetails = output<number>();
  
  isLoading = signal(true);
  loadError = signal<string | null>(null);
  

  center = signal<google.maps.LatLngLiteral>({ lat: -26.2041, lng: 28.0473 });
  mapOptions = signal<google.maps.MapOptions>({
    mapTypeId: 'roadmap',
    zoomControl: true,
    scrollwheel: true,
    disableDoubleClickZoom: false,
    maxZoom: 18,
    minZoom: 8,
    streetViewControl: false,
    mapTypeControl: false,
    fullscreenControl: true,
  });
  
 
  userMarkerOptions = signal<google.maps.MarkerOptions>({});
  userMarkerPosition = signal<google.maps.LatLngLiteral | null>(null);
  
 
  instituteMarkerOptions = signal<google.maps.MarkerOptions>({});
  
 
  selectedInstitute = signal<NearbyInstituteDto | null>(null);

  constructor(private mapsLoader: GoogleMapsLoaderService) {}

  async ngOnInit() {
    try {
      await this.mapsLoader.load();
      
      await this.waitForGoogleMaps();
      
      this.userMarkerOptions.set({
        icon: {
          path: google.maps.SymbolPath.CIRCLE,
          scale: 12,
          fillColor: '#4285F4',
          fillOpacity: 1,
          strokeColor: '#FFFFFF',
          strokeWeight: 3,
        },
        title: 'Your Location',
      });
      
  
      this.instituteMarkerOptions.set({
        icon: {
          path: google.maps.SymbolPath.CIRCLE,
          scale: 8,
          fillColor: '#EA4335',
          fillOpacity: 1,
          strokeColor: '#FFFFFF',
          strokeWeight: 2,
        },
      });
      
      this.isLoading.set(false);
      
      if (this.userLocation) {
        this.setUserLocation(this.userLocation);
      }
    } catch (error) {
      console.error('Failed to load Google Maps', error);
      this.loadError.set('Failed to load map. Please refresh the page.');
      this.isLoading.set(false);
    }
  }

 
  private async waitForGoogleMaps(): Promise<void> {
    let attempts = 0;
    const maxAttempts = 50; 

    while (attempts < maxAttempts) {
      if (typeof google !== 'undefined' && google.maps && google.maps.SymbolPath) {
        return Promise.resolve();
      }
      await new Promise(resolve => setTimeout(resolve, 100));
      attempts++;
    }
    
    throw new Error('Google Maps failed to initialize');
  }

  
  setUserLocation(location: MapLocation) {
    const position = { lat: location.lat, lng: location.lng };
    this.center.set(position);
    this.userMarkerPosition.set(position);
  }

  
  centerOnUser() {
    if (this.userMarkerPosition()) {
      this.center.set(this.userMarkerPosition()!);
      if (this.map?.googleMap) {
        this.map.googleMap.panTo(this.userMarkerPosition()!);
      }
    }
  }
  
 
  getInstitutePosition(institute: NearbyInstituteDto): google.maps.LatLngLiteral {
    return { lat: institute.latitude, lng: institute.longitude };
  }

  openInfoWindow(marker: MapMarker, institute: NearbyInstituteDto) {
    this.selectedInstitute.set(institute);
    this.infoWindow.open(marker);
  }
  
 
  onViewDetails() {
    const institute = this.selectedInstitute();
    if (institute) {
      this.viewDetails.emit(institute.id);
    }
  }
  
  
  getStatusClass(institute: NearbyInstituteDto): string {
    if (institute.isAccredited) return 'status-accredited';
    if (institute.providerType?.includes('Provisional')) return 'status-provisional';
    return 'status-not-accredited';
  }
}