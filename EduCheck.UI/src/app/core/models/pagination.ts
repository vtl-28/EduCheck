export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface NearbyInstituteDto {
  id: number;
  institutionName: string;
  providerType: string | null;
  physicalAddress: string | null;
  city: string | null;
  province: string | null;
  latitude: number;
  longitude: number;
  distance: number;
  isAccredited: boolean;
}

export interface NearbySearchParams {
  lat: number;
  lng: number;
  radius: number;
  page?: number;
  pageSize?: number;
}