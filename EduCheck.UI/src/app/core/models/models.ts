
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[] | null;
}


export interface Institute {
  id: number;
  institutionName: string;
  accreditationNumber: string;
  accreditationPeriod: string;
  providerType: string;
  postalAddress: string;
  physicalAddress: string;
  telephone: string;
  province: string | null;
  city: string | null;
  isAccredited: boolean;
}

export type AccreditationStatus = 'Accredited' | 'Provisional' | 'NotAccredited';

export function getStatus(institute: Institute): AccreditationStatus {
  if (institute.isAccredited) return 'Accredited';
  if (institute.providerType?.toLowerCase().includes('provisional')) return 'Provisional';
  return 'NotAccredited';
}

export interface Pagination {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}


export interface SearchResponse {
  institutes: Institute[];
  pagination: Pagination;
}

export interface SearchHistoryEntry {
  id: number;
  searchedAt: string;
  institute: Institute;
}

export interface SearchHistoryResponse {
  history: SearchHistoryEntry[];
  pagination: Pagination;
}


export interface FavoriteEntry {
  id: number;
  institute: Institute;
  favoritedAt?: string;
}

export interface FavoritesResponse {
  favorites: FavoriteEntry[];
  pagination: Pagination;
}

export interface FavoriteStatusResponse {
  isFavorited: boolean;
  favoriteId: number | null;
  favoritedAt: string | null;
}


export type ReportStatus =
  | 'Submitted'
  | 'UnderReview'
  | 'Verified'
  | 'Dismissed'
  | 'ActionTaken';

export type ReportSeverity = 'Low' | 'Medium' | 'High' | 'Critical';

export interface FraudReportReporter {
  studentId: string;
  fullName: string;
  email: string;
}

export interface FraudReport {
  id: string;
  reportedInstituteName: string;
  reportedInstituteAddress: string;
  reportedInstitutePhone: string | null;
  description: string;
  status: ReportStatus;
  severity: ReportSeverity;
  createdAt: string;
  updatedAt: string;
  reporter: FraudReportReporter;
}

export interface FraudReportsResponse {
  reports: FraudReport[];
  pagination: Pagination;
}

export interface FraudReportStatistics {
  totalReports: number;
  submittedCount: number;
  underReviewCount: number;
  verifiedCount: number;
  dismissedCount: number;
  actionTakenCount: number;
  reportsToday: number;
  reportsThisWeek: number;
  reportsThisMonth: number;
}

export interface CreateFraudReportRequest {
  reportedInstituteName: string;
  reportedInstituteAddress: string;
  reportedInstitutePhone?: string | null;
  description: string;
}