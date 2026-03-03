import {
  Component,
  signal,
  computed,
  OnInit,
  inject,
  HostListener,
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { debounceTime, distinctUntilChanged, Subject, switchMap, EMPTY } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { InstituteService } from '../../core/services/institute.service';
import { AuthService } from '../../core/services/auth.service';
import { AnalyticsService } from '../../core/services/analytics';
import { Institute, AccreditationStatus, getStatus } from '../../core/models/models';
import { Drawer } from '../../shared/components/drawer/drawer';

const SA_PROVINCES = [
  'Eastern Cape', 'Free State', 'Gauteng', 'KwaZulu-Natal',
  'Limpopo', 'Mpumalanga', 'North West', 'Northern Cape', 'Western Cape',
];

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [FormsModule, MatSnackBarModule, Drawer],
  templateUrl: './search.html',
  styleUrl: './search.scss',
})
export class Search implements OnInit {
  private instituteService = inject(InstituteService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private snackBar = inject(MatSnackBar);
  private analytics = inject(AnalyticsService);

  readonly displayName = this.auth.displayName;
  readonly userInitials = this.auth.userInitials;

  drawerOpen = signal(false);
  searchQuery = signal('');
  selectedProvince = signal('');
  results = signal<Institute[]>([]);
  loading = signal(false);
  hasSearched = signal(false);
  favoriteIds = signal<Set<string>>(new Set());

  readonly greeting = computed(() => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 17) return 'Good afternoon';
    return 'Good evening';
  });

  readonly provinces = ['All Provinces', ...SA_PROVINCES];

  private searchSubject = new Subject<string>();

  constructor() {
    this.searchSubject
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        switchMap((query) => {
          if (!query.trim()) {
            this.results.set([]);
            this.hasSearched.set(false);
            this.loading.set(false);
            return EMPTY;
          }

          // Track search initiation
          this.analytics.trackEvent('institute_search_initiated', {
            query: query,
            query_length: query.length,
            has_province_filter: this.selectedProvince() !== 'All Provinces' && !!this.selectedProvince(),
            province: this.selectedProvince() || 'all'
          });

          this.loading.set(true);
          const province =
            this.selectedProvince() === 'All Provinces'
              ? undefined
              : this.selectedProvince() || undefined;
          return this.instituteService.search(query, province);
        }),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (res) => {
          this.results.set(res.institutes);
          this.hasSearched.set(true);
          this.loading.set(false);

          const unaccreditedCount = res.institutes.filter(i => !i.isAccredited).length;
          const provisionalCount = res.institutes.filter(i => getStatus(i) === 'Provisional').length;

          // Track search completion
          this.analytics.trackEvent('institute_search_completed', {
            query: this.searchQuery(),
            results_count: res.institutes.length,
            has_results: res.institutes.length > 0,
            unaccredited_count: unaccreditedCount,
            provisional_count: provisionalCount,
            province_filter: this.selectedProvince() || 'all'
          });

          // Track fraud discovery
          if (unaccreditedCount > 0) {
            this.analytics.trackEvent('unaccredited_institute_discovered', {
              query: this.searchQuery(),
              unaccredited_count: unaccreditedCount,
              total_results: res.institutes.length,
              institute_names: res.institutes
                .filter(i => !i.isAccredited)
                .map(i => i.institutionName)
            });
          }
        },
        error: (err) => {
          this.loading.set(false);

          // Track search error
          this.analytics.trackEvent('institute_search_failed', {
            query: this.searchQuery(),
            error_message: err?.message || 'Unknown error',
            province_filter: this.selectedProvince() || 'all'
          });

          this.snackBar.open('Search failed. Please try again.', 'Dismiss', { duration: 3000 });
        },
      });

    // Track page view on initialization
    this.analytics.trackPageView('search', {
      is_authenticated: this.auth.isAuthenticated()
    });
  }

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap.get('q');
    if (q) {
      this.searchQuery.set(q);
      this.searchSubject.next(q);

      // Track search from URL query param
      this.analytics.trackEvent('search_from_url_param', {
        query: q
      });
    }
  }

  onSearchInput(value: string): void {
    this.searchQuery.set(value);
    this.searchSubject.next(value);
  }

  clearSearch(): void {
    this.analytics.trackEvent('search_cleared', {
      had_query: !!this.searchQuery(),
      had_results: this.results().length > 0
    });

    this.searchQuery.set('');
    this.results.set([]);
    this.hasSearched.set(false);
  }

  selectProvince(province: string): void {
    this.analytics.trackEvent('province_filter_changed', {
      previous_province: this.selectedProvince() || 'all',
      new_province: province,
      has_active_search: !!this.searchQuery().trim()
    });

    this.selectedProvince.set(province);
    if (this.searchQuery().trim()) {
      this.searchSubject.next(this.searchQuery());
    }
  }

  openInstitute(result: Institute): void {
    this.analytics.trackEvent('institute_clicked', {
      institute_id: result.id,
      institute_name: result.institutionName,
      is_accredited: result.isAccredited,
      accreditation_status: getStatus(result),
      provider_type: result.providerType,
      province: result.province,
      source: 'search_results'
    });

    this.router.navigate(['/institutes', result.id]);
  }

  openFromDrawer(instituteId: string): void {
    this.analytics.trackEvent('institute_opened_from_drawer', {
      institute_id: instituteId
    });

    this.drawerOpen.set(false);
    this.router.navigate(['/institutes', instituteId]);
  }

  toggleFavorite(result: Institute, event: Event): void {
    event.stopPropagation();
    
    const favs = new Set(this.favoriteIds());
    const key = result.id.toString();
    const isCurrentlyFavorite = favs.has(key);

    if (isCurrentlyFavorite) {
      this.instituteService.removeFavorite(result.id).subscribe(() => {
        favs.delete(key);
        this.favoriteIds.set(new Set(favs));

        // Track unfavorite
        this.analytics.trackEvent('institute_unfavorited', {
          institute_id: result.id,
          institute_name: result.institutionName,
          is_accredited: result.isAccredited,
          source: 'search_results'
        });
      });
    } else {
      this.instituteService.addFavorite(result.id).subscribe(() => {
        favs.add(key);
        this.favoriteIds.set(new Set(favs));

        // Track favorite
        this.analytics.trackEvent('institute_favorited', {
          institute_id: result.id,
          institute_name: result.institutionName,
          is_accredited: result.isAccredited,
          accreditation_status: getStatus(result),
          source: 'search_results'
        });
      });
    }
  }

  isFavorite(id: number): boolean {
    return this.favoriteIds().has(id.toString());
  }

  reportInstitute(result: Institute, event: Event): void {
    event.stopPropagation();

    this.analytics.trackEvent('report_institute_clicked', {
      institute_id: result.id,
      institute_name: result.institutionName,
      is_accredited: result.isAccredited,
      accreditation_status: getStatus(result),
      source: 'search_results'
    });

    this.router.navigate(['/report', result.id]);
  }

  reportUnknown(): void {
    this.analytics.trackEvent('report_unknown_institute_clicked', {
      search_query: this.searchQuery(),
      source: 'no_results'
    });

    this.router.navigate(['/report'], {
      queryParams: { name: this.searchQuery() },
    });
  }

  badgeClass(institute: Institute): string {
    const map: Record<AccreditationStatus, string> = {
      Accredited: 'badge--green',
      Provisional: 'badge--amber',
      NotAccredited: 'badge--red',
    };
    return map[getStatus(institute)];
  }

  badgeLabel(institute: Institute): string {
    const map: Record<AccreditationStatus, string> = {
      Accredited: '✓ Accredited',
      Provisional: '⏳ Provisional',
      NotAccredited: '✕ Not Accredited',
    };
    return map[getStatus(institute)];
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.drawerOpen()) {
      this.analytics.trackEvent('drawer_closed', {
        method: 'escape_key'
      });
    }
    this.drawerOpen.set(false);
  }
}