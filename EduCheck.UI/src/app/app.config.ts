import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    // Router — withComponentInputBinding lets route params bind directly
    // to component @Input() properties (e.g. :id → @Input() id)
    provideRouter(routes, withComponentInputBinding()),

    // HTTP client with auth interceptor (attaches JWT to every request)
    provideHttpClient(withInterceptors([authInterceptor])),

    // Angular Material animations
    provideAnimationsAsync(),
  ],
};