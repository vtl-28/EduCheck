import { Component, OnInit, signal } from '@angular/core';
import { Router, RouterOutlet, NavigationEnd } from '@angular/router';
import { ToastService } from './shared/services/toast';
import { filter } from 'rxjs/operators';
import { AnalyticsService } from './core/services/analytics';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class AppComponent implements OnInit {
  protected readonly title = signal('EduCheck');
  constructor(
    public toastService: ToastService,
    public router: Router,
    public analytics: AnalyticsService
  ) {}

  ngOnInit(): void {
    // Track route changes
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      // PostHog auto-captures pageviews, but we can add custom properties
      const routeName = this.getRouteNameFromUrl(event.urlAfterRedirects);
      
      this.analytics.trackPageView(routeName, {
        path: event.urlAfterRedirects,
        url: event.url
      });
    });
  }

  private getRouteNameFromUrl(url: string): string {
    // Convert URL to readable route name
    if (url === '/' || url === '') return 'home';
    if (url.includes('/search')) return 'search';
    if (url.includes('/nearby')) return 'nearby';
    if (url.includes('/institute')) return 'institute_details';
    if (url.includes('/auth/login')) return 'login';
    if (url.includes('/auth/register')) return 'register';
    return url.replace(/^\//, '').replace(/\//g, '_');
  }
}

