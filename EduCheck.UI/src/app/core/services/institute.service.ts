import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  Institute,
  SearchResponse,
  SearchHistoryEntry,
  SearchHistoryResponse,
  FavoriteEntry,
  FavoritesResponse,
  FavoriteStatusResponse,
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class InstituteService {
  private readonly base = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  search(query: string, province?: string, page = 1, pageSize = 20): Observable<SearchResponse> {
    let params = new HttpParams()
      .set('query', query)
      .set('page', page)
      .set('pageSize', pageSize);
    if (province) params = params.set('province', province);
    return this.http
      .get<ApiResponse<SearchResponse>>(`${this.base}/Institutes/search`, { params })
      .pipe(map((res) => res.data));
  }

  getById(id: number | string): Observable<Institute> {
    return this.http
      .get<ApiResponse<Institute>>(`${this.base}/Institutes/${id}`)
      .pipe(map((res) => res.data));
  }

  getSearchHistory(): Observable<SearchHistoryEntry[]> {
    return this.http
      .get<ApiResponse<SearchHistoryResponse>>(`${this.base}/search-history`)
      .pipe(map((res) => res.data.history));
  }

  deleteHistoryEntry(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/search-history/${id}`);
  }

  getFavorites(): Observable<FavoriteEntry[]> {
    return this.http
      .get<ApiResponse<FavoritesResponse>>(`${this.base}/favorites`)
      .pipe(map((res) => res.data.favorites));
  }


  addFavorite(instituteId: number): Observable<void> {
    return this.http.post<void>(`${this.base}/favorites/${instituteId}`, {});
  }

  removeFavorite(instituteId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/favorites/${instituteId}`);
  }

  checkIsFavorite(instituteId: number): Observable<FavoriteStatusResponse> {
    return this.http
      .get<ApiResponse<FavoriteStatusResponse>>(`${this.base}/favorites/${instituteId}/status`)
      .pipe(map((res) => res.data));
  }
}