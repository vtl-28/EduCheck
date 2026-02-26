import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: 'Student' | 'Admin' | number;
  province?: string | null;
  city?: string | null;
  phoneNumber?: string | null;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  province?: string;
  phoneNumber?: string;
  city?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'educheck_access_token';
  private readonly REFRESH_KEY = 'educheck_refresh_token';
  private readonly USER_KEY = 'educheck_user';

  private _currentUser = signal<User | null>(this.loadUserFromStorage());

  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);
  readonly isAdmin = computed(() => this._currentUser()?.role === 'Admin');
  readonly isStudent = computed(() => this._currentUser()?.role === 'Student');
  readonly userInitials = computed(() => {
    const user = this._currentUser();
    if (!user) return '';
    return `${user.firstName[0]}${user.lastName[0]}`.toUpperCase();
  });
  readonly displayName = computed(() => {
    const user = this._currentUser();
    if (!user) return '';
    return `${user.firstName} ${user.lastName}`;
  });

  constructor(private http: HttpClient, private router: Router) {}

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/Auth/login`, credentials)
      .pipe(tap((res) => this.handleAuthSuccess(res)));
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    console.log('Registering user with data:', data);
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/Auth/register/student`, data)
      .pipe(tap((res) => this.handleAuthSuccess(res)));
  }

  handleOAuthSuccess(
  accessToken: string,
  refreshToken: string,
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    fullName: string;
    role: 'Student' | 'Admin';
  }
): void {
  this.storeTokens(accessToken, refreshToken);
  this._currentUser.set(user);
  localStorage.setItem(this.USER_KEY, JSON.stringify(user));
}


  loginWithGoogle(): void {
  // Step 1: Get the Google authorization URL from the backend
  this.http
    .get<{ authorizationUrl: string }>(`${environment.apiUrl}/Auth/google-login`)
    .subscribe({
      next: (res) => {
        // Step 2: Redirect browser to Google consent screen
        window.location.href = res.authorizationUrl;
      },
      error: () => {
        console.error('Could not get Google authorization URL');
      },
    });
}

  handleOAuthCallback(token: string, refreshToken: string, user: User): void {
    this.storeTokens(token, refreshToken);
    this._currentUser.set(user);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
    this._currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_KEY);
  }

  refreshAccessToken(): Observable<AuthResponse> {
    const refreshToken = this.getRefreshToken();
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/Auth/refresh-token`, { refreshToken })
      .pipe(tap((res) => this.handleAuthSuccess(res)));
  }


  private handleAuthSuccess(res: AuthResponse): void {
   this.storeTokens(res.accessToken, res.refreshToken);
  // Map numeric role to string
  const user = {
    ...res.user,
    role: this.mapRole(res.user.role),
  };
  this._currentUser.set(user);
  localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private mapRole(role: number | string): 'Student' | 'Admin' {
  if (role === 0 || role === 'Student') return 'Student';
  if (role === 1 || role === 'Admin') return 'Admin';
  return 'Student';
}

  private storeTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(this.TOKEN_KEY, accessToken);
    localStorage.setItem(this.REFRESH_KEY, refreshToken);
  }

  private loadUserFromStorage(): User | null {
    try {
      const raw = localStorage.getItem(this.USER_KEY);
      return raw ? (JSON.parse(raw) as User) : null;
    } catch {
      return null;
    }
  }

  updateCurrentUser(user: User): void {
    this._currentUser.set(user);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }
}