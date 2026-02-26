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
        },
        error: () => {
          this.loading.set(false);
          this.snackBar.open('Search failed. Please try again.', 'Dismiss', { duration: 3000 });
        },
      });
  }

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap.get('q');
    if (q) {
      this.searchQuery.set(q);
      this.searchSubject.next(q);
    }
  }

  onSearchInput(value: string): void {
    this.searchQuery.set(value);
    this.searchSubject.next(value);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.results.set([]);
    this.hasSearched.set(false);
  }

  selectProvince(province: string): void {
    this.selectedProvince.set(province);
    if (this.searchQuery().trim()) {
      this.searchSubject.next(this.searchQuery());
    }
  }

  openInstitute(result: Institute): void {
    this.router.navigate(['/institutes', result.id]);
  }

  openFromDrawer(instituteId: string): void {
    this.drawerOpen.set(false);
    this.router.navigate(['/institutes', instituteId]);
  }

  toggleFavorite(result: Institute, event: Event): void {
    event.stopPropagation();
    const favs = new Set(this.favoriteIds());
    const key = result.id.toString();
    if (favs.has(key)) {
      this.instituteService.removeFavorite(result.id).subscribe(() => {
        favs.delete(key);
        this.favoriteIds.set(new Set(favs));
      });
    } else {
      this.instituteService.addFavorite(result.id).subscribe(() => {
        favs.add(key);
        this.favoriteIds.set(new Set(favs));
      });
    }
  }

  isFavorite(id: number): boolean {
    return this.favoriteIds().has(id.toString());
  }

  reportInstitute(result: Institute, event: Event): void {
    event.stopPropagation();
    this.router.navigate(['/report', result.id]);
  }

  reportUnknown(): void {
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
    this.drawerOpen.set(false);
  }
}