import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { adminGuard } from './core/guards/admin-guard';
import { guestGuard } from './core/guards/guest-guard';
import { LocationTestComponent } from './features/nearby-institutes/location-test';
import { NearbyInstitutesComponent } from './features/nearby-institutes/nearby-institutes';

export const routes: Routes = [

  {
    path: '',
    loadComponent: () =>
      import('./features/landing/landing').then(
        (m) => m.Landing
      ),
  },

  {
    path: 'auth/login',
    loadComponent: () =>
      import('./features/auth/login/login').then(
        (m) => m.Login
      ),
    canActivate: [guestGuard],
  },

  {
    path: 'auth/register',
    loadComponent: () =>
      import('./features/auth/register/register').then(
        (m) => m.Register
      ),
    canActivate: [guestGuard],
  },
  {
  path: 'auth/google-callback',
  loadComponent: () =>
    import('./features/auth/google-callback/google-callback')
    .then(m => m.GoogleCallback),
  },

  {
    path: 'search',
    loadComponent: () =>
      import('./features/search/search').then(
        (m) => m.Search
      ),
    canActivate: [authGuard],
  },

  {
    path: 'institutes/:id',
    loadComponent: () =>
      import('./features/institute-detail/institute-detail').then(
        (m) => m.InstituteDetail
      ),
    canActivate: [authGuard],
  },

  {
    path: 'favorites',
    loadComponent: () =>
      import('./features/favorites/favorites').then(
        (m) => m.Favorites
      ),
    canActivate: [authGuard],
  },

  {
    path: 'profile',
    loadComponent: () =>
      import('./features/profile/profile').then(
        (m) => m.Profile
      ),
    canActivate: [authGuard],
  },

  {
    path: 'report',
    loadComponent: () =>
      import('./features/search/report/report').then(
        (m) => m.Report
      ),
    canActivate: [authGuard],
  },

  {
    path: 'report/:instituteId',
    loadComponent: () =>
      import('./features/search/report/report').then(
        (m) => m.Report
      ),
    canActivate: [authGuard],
  },
  {
    path: 'location-test',
    component: LocationTestComponent
  },
   {
    path: 'nearby',
    component: NearbyInstitutesComponent
  },

  {
    path: 'admin/reports',
    loadComponent: () =>
      import('./features/admin/reports/reports').then(
        (m) => m.Reports
      ),
    canActivate: [authGuard, adminGuard],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/not-found/not-found').then(
        (m) => m.NotFound
      ),
  },
];