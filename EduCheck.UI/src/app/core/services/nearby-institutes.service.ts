import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PaginatedResponse, NearbyInstituteDto, NearbySearchParams } from '../models/pagination';

@Injectable({
  providedIn: 'root'
})
export class NearbyInstitutesService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Institutes/nearby`;

  /**
   * Search for institutes near a location
   */
  getNearby(params: NearbySearchParams): Observable<PaginatedResponse<NearbyInstituteDto>> {
    let httpParams = new HttpParams()
      .set('lat', params.lat.toString())
      .set('lng', params.lng.toString())
      .set('radius', params.radius.toString());

    if (params.page) {
      httpParams = httpParams.set('page', params.page.toString());
    }

    if (params.pageSize) {
      httpParams = httpParams.set('pageSize', params.pageSize.toString());
    }

    return this.http.get<PaginatedResponse<NearbyInstituteDto>>(this.apiUrl, { params: httpParams });
  }
}