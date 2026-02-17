import {
  HttpInterceptorFn,
  HttpErrorResponse,
  HttpRequest,
  HttpHandlerFn,
  HttpEvent,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  catchError,
  switchMap,
  throwError,
  BehaviorSubject,
  filter,
  take,
  Observable,
} from 'rxjs';
import { AuthService } from '../services/auth.service';

// Shared refresh state — outside function so shared across concurrent requests
let isRefreshing = false;
const refreshToken$ = new BehaviorSubject<string | null>(null);

function addToken(
  req: HttpRequest<unknown>,
  token: string
): HttpRequest<unknown> {
  return req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });
}

function isAuthEndpoint(url: string): boolean {
  return (
    url.includes('/Auth/login') ||
    url.includes('/Auth/register') ||
    url.includes('/Auth/refresh-token') ||
    url.includes('/Auth/google-login') ||
    url.includes('/Auth/google-callback')
  );
}

function handle401(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  auth: AuthService,
  router: Router
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshToken$.next(null);

    return auth.refreshAccessToken().pipe(
      switchMap((res) => {
        isRefreshing = false;
        refreshToken$.next(res.accessToken);
        return next(addToken(req, res.accessToken));
      }),
      catchError((err) => {
        isRefreshing = false;
        refreshToken$.next(null);
        auth.logout();
        router.navigate(['/auth/login']);
        return throwError(() => err);
      })
    );
  }

  // Other concurrent requests wait for the new token then retry
  return refreshToken$.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap((token) => next(addToken(req, token)))
  );
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (isAuthEndpoint(req.url)) {
    return next(req);
  }

  const token = auth.getAccessToken();
  const authReq = token ? addToken(req, token) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return handle401(req, next, auth, router);
      }
      return throwError(() => error);
    })
  );
};